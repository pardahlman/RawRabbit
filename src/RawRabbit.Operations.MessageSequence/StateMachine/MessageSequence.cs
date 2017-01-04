using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Context;
using RawRabbit.Operations.MessageSequence.Configuration;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.Saga;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Operations.Saga.Trigger;
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
		private readonly TriggerConfigurer<MessageSequence<TMessageContext>> _triggerConfigurer;
		private readonly Queue<StepDefinition> _stepDefinitions;

		public MessageSequence(IBusClient client, SequenceModel model = null) : base(model)
		{
			_client = client;
			_triggerConfigurer = new TriggerConfigurer<MessageSequence<TMessageContext>>();
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

			_triggerConfigurer
				.FromMessage<TMessage>(
					message => SagaDto.Id,
					(sequence, message) => StateMachine.FireAsync(trigger, message),
					cfg => cfg
						.FromDeclaredQueue(q => q.WithName($"state_machine_{SagaDto.Id}"))
						.Consume(c => c.WithRoutingKey($"{typeof(TMessage).Name.ToLower()}.{SagaDto.Id}")
					)
				);
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
					sequence.Completed = SagaDto.Completed;
					sequence.Skipped = SagaDto.Skipped;
					sequence.Aborted = true;
					tsc.TrySetResult(default(TMessage));
				});

			_triggerConfigurer
				.FromMessage<TMessage>(
					message => SagaDto.Id,
					(s, message) => StateMachine.FireAsync(trigger, message),
					cfg => cfg
						.FromDeclaredQueue(q => q.WithName($"state_machine_{SagaDto.Id}"))
						.Consume(c => c.WithRoutingKey($"{typeof(TMessage).Name.ToLower()}.{SagaDto.Id}")
						)
				);

			foreach (var invoker in _triggerConfigurer.SagaSubscribeOptions)
			{
				_client.InvokeAsync(p => p
							.Use<ConsumeConfigurationMiddleware>()
							.Use<QueueDeclareMiddleware>()
							.Use<ExchangeDeclareMiddleware>()
							.Use<QueueBindMiddleware>(),
						context =>
						{
							invoker.ContextActionFunc?.Invoke(context)?.Invoke(context);
						}
					)
					.GetAwaiter()
					.GetResult();
			}
			Func<object[], Task> genericHandler = args => TriggerAsync(args[1].GetType(), args[1]);
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
						Pipe = TriggerConfigurer<MessageSequence<TMessageContext>>.ConsumePipe
					}),
				context =>
				{
					context.Properties.Add(SagaKey.SagaType, GetType());
					context.Properties.Add(SagaKey.SagaId, SagaDto.Id);
					context.Properties.Add(SagaKey.Saga, this);
					context.Properties.Add(PipeKey.MessageHandler, genericHandler);
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
