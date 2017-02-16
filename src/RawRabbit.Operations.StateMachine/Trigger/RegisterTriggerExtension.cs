using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.StateMachine.Trigger
{
	public static class RegisterTriggerExtension
	{
		public static readonly Action<IPipeBuilder> ConsumePipe =
			SubscribeMessageExtension.ConsumePipe + (p => p
				.Use<ModelIdMiddleware>()
				.Use<GlobalLockMiddleware>()
				.Use<RetrieveStateMachineMiddleware>()
				.Replace<SubscriptionExceptionMiddleware, HandlerInvokationMiddleware>(args: new HandlerInvokationOptions
				{
					HandlerArgsFunc = context => context.GetLazyHandlerArgs()
				})
		);

		public static readonly Action<IPipeBuilder> SubscribePipe =
			SubscribeMessageExtension.SubscribePipe + (p => p
			.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions { Pipe = ConsumePipe })
		);

		public static async Task RegisterTrigger(this IBusClient client, Action<IPipeContext> context, CancellationToken ct = default(CancellationToken))
		{
			await client.InvokeAsync(SubscribePipe, context, ct);
		}

		public static Task RegisterMessageTrigger<TStateMachine, TMessage>(
			this IBusClient client,
			Func<TMessage, Guid> correlationFunc,
			Func<TStateMachine, TMessage, Task> machineFunc,
			Action<IConsumerConfigurationBuilder> consumeConfig = null)
		{
			Func<object[], Task> genericHandler = args => machineFunc((TStateMachine)args[0], (TMessage)args[1]).ContinueWith<Acknowledgement>(t => new Ack());
			Func<object, Guid> genericCorrFunc = o => correlationFunc((TMessage)o);
			return client.RegisterTrigger(context =>
			{
				context.Properties.Add(StateMachineKey.CorrelationFunc, genericCorrFunc);
				context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
				context.Properties.Add(PipeKey.ConfigurationAction, consumeConfig);
				context.Properties.Add(PipeKey.MessageHandler, genericHandler);
				context.UseLazyHandlerArgs(ctx => new[] {ctx.GetStateMachine(), ctx.GetMessage()});
			});
		}
	}
}
