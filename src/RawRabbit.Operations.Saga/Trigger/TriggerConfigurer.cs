using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Saga.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Saga.Trigger
{
	public class TriggerConfigurer<TSaga> where TSaga : Model.Saga
	{
		public List<SagaSubscriberOptions> SagaSubscribeOptions { get; set; }

		public static readonly Action<IPipeBuilder> ConsumePipe = pipe => pipe
			.Use<BodyDeserializationMiddleware>()
			.Use<SagaIdMiddleware>()
			.Use<GlobalLockMiddleware>()
			.Use<RetrieveSagaMiddleware>()
			.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
			{
				HeaderKey = PropertyHeaders.GlobalExecutionId,
				Type = typeof(string),
				ContextSaveAction = (ctx, id) => ctx.Properties.TryAdd(PipeKey.GlobalExecutionId, id)
			})
			.Use<GlobalExecutionIdMiddleware>()
			.Use<HandlerInvokationMiddleware>(new HandlerInvokationOptions
			{
				HandlerArgsFunc = context => new[] { context.GetSaga(), context.GetMessage() }
			})
			.Use<AutoAckMiddleware>();

		public static readonly Action<IPipeBuilder> AutoAckPipe = SubscribeMessageExtension.AutoAckPipe + (builder => builder
			.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
			{
				Pipe = ConsumePipe
			}));

		public TriggerConfigurer()
		{
			SagaSubscribeOptions = new List<SagaSubscriberOptions>();
		}

		public TriggerConfigurer<TSaga> FromMessage<TMessage>(
			Func<TMessage, Guid> correlationFunc,
			Action<TSaga, TMessage> sagaAction,
			Action<IConsumerConfigurationBuilder> consumeConfig = null)
		{
			return FromMessage(
				correlationFunc, (saga, message) =>
				{
					sagaAction(saga, message);
					return Task.FromResult(0);
				},
				consumeConfig);
		}

		public TriggerConfigurer<TSaga> FromMessage<TMessage>(
			Func<TMessage, Guid> correlationFunc,
			Func<TSaga, TMessage, Task> sagaFunc,
			Action<IConsumerConfigurationBuilder> consumeConfig = null)
		{
			Func<object[], Task> genericHandler = args => sagaFunc((TSaga)args[0], (TMessage)args[1]);
			Func<object, Guid> genericCorrFunc = o => correlationFunc((TMessage) o);

			SagaSubscribeOptions.Add(new SagaSubscriberOptions
			{
				ContextActionFunc = c=>  context =>
				{
					context.Properties.Add(SagaKey.CorrelationFunc, genericCorrFunc);
					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					context.Properties.Add(PipeKey.ConfigurationAction, consumeConfig);
					context.Properties.Add(PipeKey.MessageHandler, genericHandler);
				},
				PipeActionFunc = c =>  AutoAckPipe
			});
			return this;
		}
	}
}
