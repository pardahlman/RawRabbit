using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration;
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
		private readonly ILegacyBusClient<TMessageContext> _busClient;
		private readonly IMessageChainTopologyUtil _chainTopology;
		private readonly IMessageChainDispatcher _dispatcher;
		private readonly IMessageSequenceRepository _repository;
		private readonly RawRabbitConfiguration _mainCfg;

		private Func<Task> _publishAsync;
		private Guid _globalMessageId ;

		public MessageSequenceBuilder(ILegacyBusClient<TMessageContext> legacyBusClient, IMessageChainTopologyUtil chainTopology, IMessageChainDispatcher dispatcher, IMessageSequenceRepository repository, RawRabbitConfiguration mainCfg)
		{
			_busClient = legacyBusClient;
			_chainTopology = chainTopology;
			_dispatcher = dispatcher;
			_repository = repository;
			_mainCfg = mainCfg;
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
			_dispatcher.AddMessageHandler(_globalMessageId, func, optionBuilder.Configuration);
			var bindTask = _chainTopology.BindToExchange<TMessage>(_globalMessageId);
			bindTask.ConfigureAwait(false);
			Task.WaitAll(bindTask);
			_logger.LogDebug($"Sucessfully bound Sequence Queue for GlobalMessageId '{_globalMessageId}' of type '{typeof(TMessage).Name}.");
			return this;
		}

		public MessageSequence<TMessage> Complete<TMessage>()
		{
			_logger.LogDebug($"Message Sequence for '{_globalMessageId}' completes with '{typeof(TMessage).Name}'.");
			var sequenceDef = _repository.GetOrCreate(_globalMessageId);
			
			var messageTcs = new TaskCompletionSource<TMessage>();
			var sequence = new MessageSequence<TMessage>
			{
				Task = messageTcs.Task
			};

			sequenceDef.TaskCompletionSource.Task.ContinueWith(tObj =>
			{
				UpdateSequenceFinalState(sequence);
				messageTcs.TrySetResult((TMessage) tObj.Result);
			});

			Func<TMessage, TMessageContext, Task> func = (message, context) =>
			{
				Task
					.WhenAll(sequenceDef.State.HandlerTasks)
					.ContinueWith(t => sequenceDef.TaskCompletionSource.TrySetResult(message));
				return Task.FromResult(true);
			};

			var bindTask = _chainTopology.BindToExchange<TMessage>(_globalMessageId);
			_dispatcher.AddMessageHandler(_globalMessageId, func);

			Task
				.WhenAll(bindTask)
				.ContinueWith(t => _publishAsync())
				.Unwrap()
				.Wait();

			Timer timeoutTimer = null;
			timeoutTimer = new Timer(state =>
			{
				timeoutTimer?.Dispose();
				var seq = _repository.Get(_globalMessageId);
				if (seq != null)
				{
					seq.State.Aborted = true;
					_repository.Update(seq);
					UpdateSequenceFinalState(sequence);
				}
				
				messageTcs.TrySetException(
					new TimeoutException(
						$"Unable to complete sequence {_globalMessageId} in {_mainCfg.RequestTimeout.ToString("g")}. Operation Timed out."));
			}, null, _mainCfg.RequestTimeout, new TimeSpan(-1));

			return sequence;
		}

		private void UpdateSequenceFinalState<TMessage>(MessageSequence<TMessage> sequence)
		{
			var final = _repository.Get(_globalMessageId);
			if (final == null)
			{
				return;
			}
			_logger.LogDebug($"Updating Sequence for '{_globalMessageId}'.");
			sequence.Aborted = final.State.Aborted;
			sequence.Completed = final.State.Completed;
			sequence.Skipped = final.State.Skipped;
			foreach (var step in final.StepDefinitions)
			{
				_chainTopology.UnbindFromExchange(step.Type, _globalMessageId);
			}
			_repository.Remove(_globalMessageId);
		}
	}
}
