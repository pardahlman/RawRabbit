using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;
using RawRabbit.Enrichers.MessageContext.Subscribe;
using RawRabbit.Enrichers.MessageContext.Subscribe.Middleware;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class SubscribeMessageContextExtension
	{
		public static readonly Action<IPipeBuilder> MessageContextPipe = SubscribeMessageExtension.SubscribePipe + (builder =>
		{
			builder
				.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
				{
					Pipe = consume => consume
						.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageRecieved))
						.Use<MessageDeserializationMiddleware>()
						.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageDeserialized))
						.Use<MessageContextDeserializationMiddleware>()
						.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageContextDeserialized))
						.Use<MessageContextEnhanceMiddleware>()
						.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageContextEnhanced))
						.Use<MessageHandlerInvokationMiddleware>()
						.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.HandlerInvoked))
				});
		});

		public static Task SubscribeAsync<TMessage, TMessageContext>(this IBusClient client, Func<TMessage, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return client
				.InvokeAsync(
					MessageContextPipe,
					context =>
					{
						Func<object, IMessageContext, Task> genericHandler =
							(msg, ctx) => subscribeMethod((TMessage) msg, (TMessageContext) ctx);

						context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
						context.Properties.Add(PipeKey.MessageHandler, genericHandler);
						context.Properties.Add(PipeKey.ConfigurationAction, configuration);
					}
				);
		}
	}
}
