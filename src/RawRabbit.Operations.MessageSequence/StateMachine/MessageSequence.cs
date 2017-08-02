using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Logging;
using RawRabbit.Operations.MessageSequence.Configuration;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.MessageSequence.Trigger;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Pipe;
using Stateless;

namespace RawRabbit.Operations.MessageSequence.StateMachine
{
	public class MessageSequence : StateMachineBase<SequenceState, Type, SequenceModel>,
			IMessageChainPublisher, IMessageSequenceBuilder
	{
		private readonly IBusClient _client;
		private readonly INamingConventions _naming;
		private readonly RawRabbitConfiguration _clientCfg;
		private Action _fireAction;
		private readonly TriggerConfigurer _triggerConfigurer;
		private readonly Queue<StepDefinition> _stepDefinitions;
		private readonly List<Subscription.ISubscription> _subscriptions;
		private readonly ILog _logger = LogProvider.For<MessageSequence>();
		private IModel _channel;

		public MessageSequence(IBusClient client, INamingConventions naming, RawRabbitConfiguration clientCfg, SequenceModel model = null) : base(model)
		{
			_client = client;
			_naming = naming;
			_clientCfg = clientCfg;
			_triggerConfigurer = new TriggerConfigurer();
			_stepDefinitions = new Queue<StepDefinition>();
			_subscriptions = new List<Subscription.ISubscription>();
		}

		protected override void ConfigureState(StateMachine<SequenceState, Type> machine)
		{
			machine
				.Configure(SequenceState.Active)
				.Permit(typeof(CancelSequence), SequenceState.Canceled);
		}

		public override SequenceModel Initialize()
		{
			return new SequenceModel
			{
				State = SequenceState.Created,
				Id = Guid.NewGuid(),
				Completed = new List<ExecutionResult>(),
				Skipped = new List<ExecutionResult>()
			};
		}

		public IMessageSequenceBuilder PublishAsync<TMessage>(TMessage message = default(TMessage), Guid globalMessageId = new Guid()) where TMessage : new()
		{
			if (globalMessageId != Guid.Empty)
			{
				_logger.Info("Setting Global Message Id to {globalMessageId}", globalMessageId);
				Model.Id = globalMessageId;
			}
			return PublishAsync(message, context => { });
		}

		public IMessageSequenceBuilder PublishAsync<TMessage>(TMessage message, Action<IPipeContext> context, CancellationToken ct = new CancellationToken())
			where TMessage : new()
		{
			_logger.Info("Initializing Message Sequence that starts with {messageType}.", typeof(TMessage).Name);

			var entryTrigger = StateMachine.SetTriggerParameters<TMessage>(typeof(TMessage));

			StateMachine
				.Configure(SequenceState.Created)
				.Permit(typeof(TMessage), SequenceState.Active);

			StateMachine
				.Configure(SequenceState.Active)
				.OnEntryFromAsync(entryTrigger, msg => _client.PublishAsync(msg, c =>
				{
					c.Properties.Add(Enrichers.GlobalExecutionId.PipeKey.GlobalExecutionId, Model.Id.ToString());
					context?.Invoke(c);
				}, ct));

			_fireAction = () => StateMachine.FireAsync(entryTrigger, message);
			return this;
		}

		public IMessageSequenceBuilder When<TMessage, TMessageContext>(Func<TMessage, TMessageContext, Task> func, Action<IStepOptionBuilder> options = null)
		{
			var optionBuilder = new StepOptionBuilder();
			options?.Invoke(optionBuilder);
			_stepDefinitions.Enqueue(new StepDefinition
			{
				Type = typeof(TMessage),
				AbortsExecution = optionBuilder.Configuration.AbortsExecution,
				Optional =  optionBuilder.Configuration.Optional
			});

			var trigger = StateMachine.SetTriggerParameters<MessageAndContext<TMessage, TMessageContext>>(typeof(TMessage));

			StateMachine
				.Configure(SequenceState.Active)
				.InternalTransitionAsync(trigger, async (message, transition) =>
				{
					_logger.Debug("Recieved message of type {messageType} for sequence {sequenceId}.", transition.Trigger.Name, Model.Id);
					var matchFound = false;
					do
					{
						if (_stepDefinitions.Peek() == null)
						{
							_logger.Info("No matching steps found for sequence. Perhaps {messageType} isn't a registered message for sequence {sequenceId}.", transition.Trigger.Name, Model.Id);
							return;
						}
						var step = _stepDefinitions.Dequeue();
						if (step.Type != typeof(TMessage))
						{
							if (step.Optional)
							{
								_logger.Info("The step for {optionalMessageType} is optional. Skipping, as recieved message is of type {currentMessageType}.", step.Type.Name, typeof(TMessage).Name);
								Model.Skipped.Add(new ExecutionResult
								{
									Type = step.Type,
									Time = DateTime.Now
								});
							}
							else
							{
								_logger.Info("The step for {messageType} is mandatory. Current message, {currentMessageType} will be dismissed.", step.Type.Name, typeof(TMessage).Name);
								return;
							}
						}
						else
						{
							matchFound = true;
						}
					} while (!matchFound);

					_logger.Debug("Invoking message handler for {messageType}", typeof(TMessage).Name);
					await func(message.Message, message.Context);
					Model.Completed.Add(new ExecutionResult
					{
						Type = typeof(TMessage),
						Time = DateTime.Now
					});
					if (optionBuilder.Configuration.AbortsExecution)
					{
						if (StateMachine.PermittedTriggers.Contains(typeof(CancelSequence)))
						{
							StateMachine.Fire(typeof(CancelSequence));
						}
					}
				});

			_triggerConfigurer
				.FromMessage<MessageSequence,TMessage, TMessageContext>(
					(msg, ctx) => Model.Id,
					(sequence, message, ctx) => StateMachine.FireAsync(trigger, new MessageAndContext<TMessage, TMessageContext> {Context = ctx, Message = message}),
					cfg => cfg
						.FromDeclaredQueue(q => q
							.WithNameSuffix(Model.Id.ToString())
							.WithExclusivity()
							.WithAutoDelete())
						.Consume(c => c.WithRoutingKey($"{_naming.RoutingKeyConvention(typeof(TMessage))}.{Model.Id}")
					)
				);
			return this;
		}

		MessageSequence<TMessage> IMessageSequenceBuilder.Complete<TMessage>()
		{
			var tsc = new TaskCompletionSource<TMessage>();
			var sequence = new MessageSequence<TMessage>
			{
				Task = tsc.Task
			};

			StateMachine
				.Configure(SequenceState.Active)
				.Permit(typeof(TMessage), SequenceState.Completed);

			StateMachine
				.Configure(SequenceState.Active)
				.OnExit(() =>
				{
					_logger.Debug("Disposing subscriptions for Message Sequence '{sequenceId}'.", Model.Id);
					foreach (var subscription in _subscriptions)
					{
						subscription.Dispose();
					}
					_channel.Dispose();
				});

			var trigger = StateMachine.SetTriggerParameters<TMessage>(typeof(TMessage));
			StateMachine
				.Configure(SequenceState.Completed)
				.OnEntryFrom(trigger, message =>
				{
					_logger.Info("Sequence {sequenceId} completed with message '{messageType}'.", Model.Id, typeof(TMessage).Name);
					sequence.Completed = Model.Completed;
					sequence.Skipped = Model.Skipped;
					tsc.TrySetResult(message);
				});

			StateMachine
				.Configure(SequenceState.Canceled)
				.OnEntry(() =>
				{
					sequence.Completed = Model.Completed;
					sequence.Skipped = Model.Skipped;
					sequence.Aborted = true;
					tsc.TrySetResult(default(TMessage));
				});

			_triggerConfigurer
				.FromMessage<MessageSequence, TMessage>(
					message => Model.Id,
					(s, message) => StateMachine.Fire(trigger, message),
					cfg => cfg
						.FromDeclaredQueue(q => q
							.WithNameSuffix(Model.Id.ToString())
							.WithExclusivity()
							.WithAutoDelete())
						.Consume(c => c.WithRoutingKey($"{_naming.RoutingKeyConvention(typeof(TMessage))}.{Model.Id}")
					)
				);

			_channel = _client.CreateChannelAsync().GetAwaiter().GetResult();

			foreach (var triggerCfg in _triggerConfigurer.TriggerConfiguration)
			{
				triggerCfg.Context += context =>
				{
					context.Properties.Add(StateMachineKey.ModelId, Model.Id);
					context.Properties.Add(StateMachineKey.Machine, this);
					context.Properties.TryAdd(PipeKey.Channel, _channel);
				};
				var ctx = _client.InvokeAsync(triggerCfg.Pipe, triggerCfg.Context).GetAwaiter().GetResult();
				_subscriptions.Add(ctx.GetSubscription());
			}

			Timer requestTimer = null;
			requestTimer = new Timer(state =>
			{
				requestTimer?.Dispose();
				tsc.TrySetException(new TimeoutException(
					$"Unable to complete sequence {Model.Id} in {_clientCfg.RequestTimeout:g}. Operation Timed out."));
				if (StateMachine.PermittedTriggers.Contains(typeof(CancelSequence)))
				{
					StateMachine.Fire(typeof(CancelSequence));
				}
			}, null, _clientCfg.RequestTimeout, new TimeSpan(-1));

			_fireAction();

			return sequence;
		}

		private class CancelSequence { }

		// Temp class until Stateless supports multiple trigger args
		private class MessageAndContext<TMessage, TContext>
		{
			public TMessage Message { get; set; }
			public TContext Context { get; set; }
		}
	}

	public enum SequenceState
	{
		Created,
		Active,
		Completed,
		Canceled
	}
}
