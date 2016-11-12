using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;
using RawRabbit.Enrichers.MessageContext.Subscribe;
using RawRabbit.Enrichers.MessageContext.Subscribe.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class SubscribeMessageContextExtension
	{
		public static readonly Action<IPipeBuilder> ConsumePipe = consume => consume
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageRecieved))
			.Use<MessageDeserializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageDeserialized))
			.Use<MessageContextDeserializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageContextDeserialized))
			.Use<MessageContextEnhancementMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.MessageContextEnhanced))
			.Use<AutoAckMessageHandlerMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(MessageContextSubscibeStage.HandlerInvoked));

		public static readonly Action<IPipeBuilder> AutoAckPipe = SubscribeMessageExtension.AutoAckPipe + (pipe =>
		{
			pipe.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions { Pipe = ConsumePipe });
		});

		public static readonly Action<IPipeBuilder> ExplicitAckPipe = AutoAckPipe + (pipe =>
			pipe.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
			{
				Pipe = ConsumePipe + (builder => builder.Replace<AutoAckMessageHandlerMiddleware, ExplicitAckMessageHandlerMiddleware>())
			})
		);

		public static Task SubscribeAsync<TMessage, TMessageContext>(this IBusClient client, Func<TMessage, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			return client
				.InvokeAsync(
					AutoAckPipe,
					context =>
					{
						Func<object, IMessageContext, Task> genericHandler = (msg, ctx) => subscribeMethod((TMessage)msg, (TMessageContext)ctx);

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
						Func<object, IMessageContext, Task> genericHandler = (msg, ctx) => subscribeMethod((TMessage)msg, (TMessageContext)ctx);

						context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
						context.Properties.Add(PipeKey.MessageHandler, genericHandler);
						context.Properties.Add(PipeKey.ConfigurationAction, configuration);
					}
				);
		}
	}
}
