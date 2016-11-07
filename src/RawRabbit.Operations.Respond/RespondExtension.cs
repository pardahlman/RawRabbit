using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Respond;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class RespondExtension
	{
		public static readonly Action<IPipeBuilder> RespondPipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.Initiated))
			.Use<RespondConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConfigurationCreated))
			.Use<QueueDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.QueueDeclared))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ExchangeDeclared))
			.Use<QueueBindMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.QueueBound))
			.Use<ChannelCreationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumerChannelCreated))
			.Use<MessageConsumeMiddleware>(new ConsumeOptions
			{
				Pipe = consume => consume
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.MessageRecieved))
					.Use<RequestDeserializationMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.MessageDeserialized))
					.Use<MessageInvokationMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.HandlerInvoked))
					.Use<BasicPropertiesMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.BasicPropertiesCreated))
					.Use<ResponseSerializationMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ResponseSerialized))
					.Use<ReplyToExtractionMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ReplyToExtracted))
					.Use<TransientChannelMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.RespondChannelCreated))
					.Use<PublishResponseMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ResponsePublished))
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumerCreated))
			.Use<SubscriptionMiddleware>();

		public static Task RespondAsync<TRequest, TResponse>(this IBusClient client, Func<TRequest, Task<TResponse>> handler, Action<IResponderConfigurationBuilder> configuration = null)
		{
			return client
				.InvokeAsync(RespondPipe, ctx =>
				{
					Func<object, Task<object>> genericHandler = o => (handler((TRequest)o).ContinueWith(tResponse => tResponse.Result as object));

					ctx.Properties.Add(RespondKey.RequestMessageType, typeof(TRequest));
					ctx.Properties.Add(RespondKey.ResponseMessageType, typeof(TResponse));
					ctx.Properties.Add(PipeKey.ConfigurationAction, configuration);
					ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
				});
		}
	}
}
