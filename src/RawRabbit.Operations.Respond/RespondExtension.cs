using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Respond;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond
{
	public static class RespondExtension
	{
		public static readonly Action<IPipeBuilder> SubscribePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.Initiated))
			.Use<ConsumeConfigurationMiddleware>()
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
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.MessageRecieved))
					.Use<RequestDeserializationMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.MessageDeserialized))
					.Use<MessageInvokationMiddleware>()
					.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.HandlerInvoked))
					.Use<ResponseSerializationMiddleware>()
					.Use<ReplyToExtractionMiddleware>()
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumerCreated))
			.Use<SubscriptionMiddleware>();
		public static Task RespondAsync<TRequest, TResponse>(Func<TRequest, Task<TResponse>> handler, Action<IResponderConfigurationBuilder> configuration = null)
		{
			return Task.FromResult(0);
		}
	}
}
