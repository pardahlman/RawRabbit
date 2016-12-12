using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Context;
using RawRabbit.Operations.MessageSequence.Configuration;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.Saga;
using RawRabbit.Operations.Saga.Middleware;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Stateless;

namespace RawRabbit.Operations.MessageSequence.StateMachine
{
	public class MessageSequence<TMessageContext> : Saga<SequenceState, Type, SequenceModel>,
			IMessageChainPublisher<TMessageContext>, IMessageSequenceBuilder<TMessageContext>
		where TMessageContext : IMessageContext, new()
	{
		private readonly IBusClient _client;
		private Action _fireAction;
		private readonly TriggerBuilder<Type> _triggerBuilder;
		private readonly Queue<StepDefinition> _stepDefinitions;

		public MessageSequence(IBusClient client, SequenceModel model = null) : base(model)
		{
			_client = client;
			_triggerBuilder = new TriggerBuilder<Type>();
			_stepDefinitions = new Queue<StepDefinition>();
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

		public IMessageSequenceBuilder<TMessageContext> PublishAsync<TMessage>(TMessage message = default(TMessage), Guid globalMessageId = new Guid()) where TMessage : new()
		{
			var entryTrigger = StateMachine.SetTriggerParameters<TMessage>(typeof(TMessage));

			StateMachine
				.Configure(SequenceState.Created)
				.Permit(typeof(TMessage), SequenceState.Active);

			StateMachine
				.Configure(SequenceState.Active)
				.OnEntryFromAsync(entryTrigger, msg => _client.PublishAsync(message, context: c => c.Properties.Add(PipeKey.GlobalExecutionId, SagaDto.Id.ToString())));

			_fireAction = () => StateMachine.FireAsync(entryTrigger, message);
			return this;
		}

		public IMessageSequenceBuilder<TMessageContext> When<TMessage>(Func<TMessage, TMessageContext, Task> func, Action<IStepOptionBuilder> options = null)
		{
			var optionBuilder = new StepOptionBuilder();
			options?.Invoke(optionBuilder);
			_stepDefinitions.Enqueue(new StepDefinition
			{
				Type = typeof(TMessage),
				AbortsExecution = optionBuilder.Configuration.AbortsExecution,
				Optional =  optionBuilder.Configuration.Optional
			});

			var trigger = StateMachine.SetTriggerParameters<TMessage>(typeof(TMessage));
			StateMachine
				.Configure(SequenceState.Active)
				.InternalTransitionAsync(trigger, (message, transition) =>
				{
					var matchFound = false;
					do
					{
						if (_stepDefinitions.Peek() == null)
						{
							return Task.FromResult(0);
						}
						var step = _stepDefinitions.Dequeue();
						if (step.Type != typeof(TMessage))
						{
							if (step.Optional)
							{
								SagaDto.Skipped.Add(new ExecutionResult
								{
									Type = step.Type,
									Time = DateTime.Now
								});
							}
							else
							{
								return Task.FromResult(0);
							}
						}
						else
						{
							matchFound = true;
						}
					} while (!matchFound);

					return func(message, default(TMessageContext))
						.ContinueWith(t =>
						{
							
							SagaDto.Completed.Add(new ExecutionResult
							{
								Type = typeof(TMessage),
								Time = DateTime.Now
							});
							if (optionBuilder.Configuration.AbortsExecution)
							{
								SagaDto.Aborted = true;
								StateMachine.Fire(typeof(CancelSequence));
							}
						});
				});

			_triggerBuilder
				.Configure(typeof(TMessage))
				.FromMessage<TMessage>(message => SagaDto.Id, cfg => cfg
					.FromDeclaredQueue(q => q.WithName($"state_machine_{SagaDto.Id}"))
					.Consume(c => c.WithRoutingKey($"{typeof(TMessage).Name.ToLower()}.{SagaDto.Id}")));
			return this;
		}

		Model.MessageSequence<TMessage> IMessageSequenceBuilder<TMessageContext>.Complete<TMessage>()
		{
			var tsc = new TaskCompletionSource<TMessage>();
			var sequence = new Model.MessageSequence<TMessage>
			{
				Task = tsc.Task
			};

			StateMachine
				.Configure(SequenceState.Active)
				.Permit(typeof(TMessage), SequenceState.Completed);

			var trigger = StateMachine.SetTriggerParameters<TMessage>(typeof(TMessage));
			StateMachine
				.Configure(SequenceState.Completed)
				.OnEntryFrom(trigger, message =>
				{
					sequence.Completed = SagaDto.Completed;
					sequence.Skipped = SagaDto.Skipped;
					tsc.TrySetResult(message);
				});

			StateMachine
				.Configure(SequenceState.Canceled)
				.OnEntry(() =>
				{
					tsc.TrySetResult(default(TMessage));
					sequence.Completed = SagaDto.Completed;
					sequence.Skipped = SagaDto.Skipped;
					sequence.Aborted = true;
				});

			_triggerBuilder
				.Configure(typeof(TMessage))
				.FromMessage<TMessage>(message => SagaDto.Id, cfg => cfg
					.FromDeclaredQueue(q => q.WithName($"state_machine_{SagaDto.Id}"))
					.Consume(c => c.WithRoutingKey($"{(typeof(TMessage).Name.ToLower())}.{SagaDto.Id}")));

			var triggerInvokers = _triggerBuilder.Build().OfType<MessageTriggerInvoker>().ToList();
			foreach (var invoker in triggerInvokers)
			{
				_client.InvokeAsync(p => p
							.Use<ConsumeConfigurationMiddleware>()
							.Use<QueueDeclareMiddleware>()
							.Use<ExchangeDeclareMiddleware>()
							.Use<QueueBindMiddleware>(),
						context =>
						{
							context.Properties.Add(SagaKey.TriggerInvoker, invoker);
							context.Properties.Add(PipeKey.ConfigurationAction, invoker.ConfigurationAction);
							context.Properties.Add(PipeKey.MessageType, invoker.MessageType);
						}
					)
					.GetAwaiter()
					.GetResult();
			}
			_client.InvokeAsync(p => p
					.Use<ConsumerMiddleware>(new ConsumerOptions
					{
						ConfigurationFunc = context => new ConsumeConfiguration
						{
							QueueName = $"state_machine_{SagaDto.Id}",
							ConsumerTag = Guid.NewGuid().ToString()
						}
					})
					.Use<MessageConsumeMiddleware>(new ConsumeOptions
					{
						Pipe = c => c
							.Use<BodyDeserializationMiddleware>()
							.Use<TriggerMessageInvokationMiddleware>(new TriggerMessageInvokationOptions
							{
								SagaFunc = context => this,
								TriggerInvokerFunc = context =>
								{
									var msgType = Type.GetType(context.GetDeliveryEventArgs().BasicProperties.Type);
									return triggerInvokers.FirstOrDefault(i => i.MessageType == msgType);
								}
							})
							.Use<AutoAckMiddleware>()
					}),
				context =>
				{
					context.Properties.Add(SagaKey.SagaType, GetType());
				});

			var requestTimeout = _client
				.InvokeAsync(builder => { })
				.ContinueWith(tContext => tContext.Result.GetClientConfiguration().RequestTimeout)
				.GetAwaiter()
				.GetResult();

			Timer requestTimer = null;
			requestTimer = new Timer(state =>
			{
				requestTimer?.Dispose();
				tsc.TrySetException(new TimeoutException(
					$"Unable to complete sequence {SagaDto.Id} in {requestTimeout:g}. Operation Timed out."));
			}, null, requestTimeout, new TimeSpan(-1));

			_fireAction();

			return sequence;
		}

		private class CancelSequence { }
	}

	public enum SequenceState
	{
		Created,
		Active,
		Completed,
		Canceled
	}
}
