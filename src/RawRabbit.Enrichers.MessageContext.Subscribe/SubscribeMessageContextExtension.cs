using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Legacy.Subscribe;
using RawRabbit.Context;
using RawRabbit.Enrichers.MessageContext.Subscribe;
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
				Type = typeof(IMessageContext),
				HeaderKey = PropertyHeaders.Context,
				ContextSaveAction = (pipeCtx, msgCtx) => pipeCtx.Properties.TryAdd(PipeKey.MessageContext, msgCtx)
			})
			.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
			{
				HeaderKey = PropertyHeaders.GlobalExecutionId,
				Type = typeof(string),
				ContextSaveAction = (ctx, id) => ctx.Properties.TryAdd(PipeKey.GlobalExecutionId, id)
			})
			.Use<GlobalExecutionIdMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageContextDeserialized))
			.Use<MessageHandlerInvokationMiddleware>(new MessageHandlerInvokationOptions { HandlerArgsFunc = context => new [] {context.GetMessage(), context.GetMessageContext()}})
			.Use<AutoAckMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.HandlerInvoked));

		public static readonly Action<IPipeBuilder> AutoAckPipe = SubscribeMessageExtension.AutoAckPipe + (pipe =>
		{
			pipe.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions { Pipe = ConsumePipe });
		});

		public static readonly Action<IPipeBuilder> ExplicitAckPipe = AutoAckPipe + (pipe =>
			pipe.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
			{
				Pipe = ConsumePipe + (builder => builder.Replace<AutoAckMiddleware, ExplicitAckMiddleware>())
			})
		);

		public static Task SubscribeAsync<TMessage, TMessageContext>(this IBusClient client, Func<TMessage, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return client
				.InvokeAsync(
					AutoAckPipe,
					context =>
					{
						Func<object[], Task> genericHandler = args => subscribeMethod((TMessage)args[0], (TMessageContext)args[1]);

						context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
						context.Properties.Add(PipeKey.MessageHandler, genericHandler);
						context.Properties.Add(PipeKey.ConfigurationAction, configuration);
					}
				);
		}

		public static Task SubscribeAsync<TMessage, TMessageContext>(this IBusClient client, Func<TMessage, TMessageContext, Task<Acknowledgement>> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return client
				.InvokeAsync(
					ExplicitAckPipe,
					context =>
					{
						Func<object[], Task> genericHandler = args => subscribeMethod((TMessage)args[0], (TMessageContext)args[1]);

						context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
						context.Properties.Add(PipeKey.MessageHandler, genericHandler);
						context.Properties.Add(PipeKey.ConfigurationAction, configuration);
					}
				);
		}
	}
}
