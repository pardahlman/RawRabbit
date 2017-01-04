using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Enrichers.MessageContext.Subscribe;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class SubscribeMessageContextExtension
	{
		public static readonly Action<IPipeBuilder> ConsumePipe = consume => consume
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageRecieved))
			.Use<BodyDeserializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageDeserialized))
			.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
			{
				HeaderTypeFunc = c => typeof(IMessageContext),
				HeaderKeyFunc = c => PropertyHeaders.Context,
				ContextSaveAction = (pipeCtx, msgCtx) => pipeCtx.Properties.TryAdd(PipeKey.MessageContext, msgCtx)
			})
			.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
			{
				HeaderKeyFunc = c => PropertyHeaders.GlobalExecutionId,
				HeaderTypeFunc = c => typeof(string),
				ContextSaveAction = (ctx, id) => ctx.Properties.TryAdd(PipeKey.GlobalExecutionId, id)
			})
			.Use<GlobalExecutionIdMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageContextDeserialized))
			.Use<SubscriptionExceptionMiddleware>(new SubscriptionExceptionOptions
			{
				InnerPipe = p => p.Use<HandlerInvokationMiddleware>(new HandlerInvokationOptions
				{
					HandlerArgsFunc = context => new[] { context.GetMessage(), context.GetMessageContext() }
				})
			})
			.Use<ExplicitAckMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.HandlerInvoked));

		public static readonly Action<IPipeBuilder> SubscribePipe = SubscribeMessageExtension.SubscribePipe + (pipe =>
		{
			pipe.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions { Pipe = ConsumePipe });
		});

		public static Task SubscribeAsync<TMessage, TMessageContext>(this IBusClient client, Func<TMessage, TMessageContext, Task> subscribeMethod, Action<IPipeContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			return client.SubscribeAsync<TMessage, TMessageContext>(
					(msg, ctx) => subscribeMethod?
						.Invoke(msg, ctx)
						.ContinueWith<Acknowledgement>(t => new Ack(), ct),
					context, ct);
		}

		public static Task SubscribeAsync<TMessage, TMessageContext>(
			this IBusClient client,
			Func<TMessage, TMessageContext, Task<Acknowledgement>> subscribeMethod,
			Action<IPipeContext> context = null,
			CancellationToken token = default(CancellationToken))
		{
			return client
				.InvokeAsync(
					SubscribePipe,
					ctx =>
					{
						Func<object[], Task> genericHandler = args => subscribeMethod((TMessage)args[0], (TMessageContext)args[1]);

						ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
						ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
						context?.Invoke(ctx);
					}, token);
		}
	}
}
