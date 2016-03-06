using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.MessageSequence.Configuration.Abstraction;
using RawRabbit.Extensions.MessageSequence.Core.Abstraction;
using RawRabbit.Extensions.MessageSequence.Model;
using RawRabbit.Extensions.MessageSequence.Repository;
using RawRabbit.Logging;

namespace RawRabbit.Extensions.MessageSequence.Configuration
{
	public class MessageSequenceBuilder<TMessageContext>
		: IMessageChainPublisher<TMessageContext>
		, IMessageSequenceBuilder<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly ILogger _logger = LogManager.GetLogger<MessageSequenceBuilder<TMessageContext>>();
		private readonly IBusClient<TMessageContext> _busClient;
		private readonly IMessageChainTopologyUtil _chainTopology;
		private readonly IMessageChainDispatcher _dispatcher;
		private readonly IMessageSequenceRepository _repository;

		private Func<Task> _publishAsync;
		private Guid _globalMessageId ;

		public MessageSequenceBuilder(IBusClient<TMessageContext> busClient, IMessageChainTopologyUtil chainTopology, IMessageChainDispatcher dispatcher, IMessageSequenceRepository repository)
		{
			_busClient = busClient;
			_chainTopology = chainTopology;
			_dispatcher = dispatcher;
			_repository = repository;
		}

		public IMessageSequenceBuilder<TMessageContext> PublishAsync<TMessage>(TMessage message = default(TMessage), Guid globalMessageId = new Guid()) where TMessage : new()
		{
			_globalMessageId = globalMessageId == Guid.Empty
				? Guid.NewGuid()
				: globalMessageId;
			_logger.LogDebug($"Preparing Message Sequence for '{_globalMessageId}' that starts with {typeof(TMessage).Name}.");
			_publishAsync = () => _busClient.PublishAsync(message, _globalMessageId);
			return this;
		}

		public IMessageSequenceBuilder<TMessageContext> When<TMessage>(Func<TMessage, TMessageContext, Task> func, Action<IStepOptionBuilder> options = null)
		{
			var optionBuilder = new StepOptionBuilder();
			options?.Invoke(optionBuilder);
			_logger.LogDebug($"Registering handler for '{_globalMessageId}' of type '{typeof(TMessage).Name}'. Optional: {optionBuilder.Configuration.Optional}, Aborts: {optionBuilder.Configuration.AbortsExecution}");
			var registerTask = _dispatcher.AddMessageHandlerAsync(_globalMessageId, func, optionBuilder.Configuration);
			var bindTask = _chainTopology.BindToExchange<TMessage>();
			Task.WaitAll(registerTask, bindTask);
			return this;
		}

		public MessageSequence<TMessage> Complete<TMessage>()
		{
			_logger.LogDebug($"Message Sequence for '{_globalMessageId}' completes with '{typeof(TMessage).Name}'.");
			var sequenceTask = _repository.GetOrCreateAsync(_globalMessageId);
			Task.WaitAll(sequenceTask);
			
			var messageTcs = new TaskCompletionSource<TMessage>();
			var sequence = new MessageSequence<TMessage>
			{
				Result = messageTcs.Task
			};

			sequenceTask.Result.TaskCompletionSource.Task.ContinueWith(tObj =>
			{
				_repository
					.GetAsync(_globalMessageId)
					.ContinueWith(tFinalSequence =>
					{
						_logger.LogDebug($"Updating Sequence for '{_globalMessageId}'.");
						sequence.Aborted = tFinalSequence.Result.State.Aborted;
						sequence.Completed = tFinalSequence.Result.State.Completed;
						sequence.Skipped = tFinalSequence.Result.State.Skipped;
						messageTcs.TrySetResult((TMessage) tObj.Result);
						return _repository.RemoveAsync(_globalMessageId);
					})
					.Unwrap();
			});

			Func<TMessage, TMessageContext, Task> func = (message, context) =>
			{
				sequenceTask.Result.TaskCompletionSource.TrySetResult(message);
				_chainTopology.Unregister(context.GlobalRequestId);
				return Task.FromResult(true);
			};

			var bindTask = _chainTopology.BindToExchange<TMessage>();
			var registerTask = _dispatcher.AddMessageHandlerAsync(_globalMessageId, func);

			Task
				.WhenAll(bindTask, registerTask)
				.ContinueWith(t => _publishAsync())
				.Unwrap()
				.Wait();

			return sequence;
		}
	}
}