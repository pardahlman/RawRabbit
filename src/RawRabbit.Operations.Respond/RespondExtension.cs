using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class RespondExtension
	{
		public static Action<IPipeBuilder> ConsumePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.MessageRecieved))
			.Use<RequestDeserializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.MessageDeserialized))
			.Use<AutoAckMessageHandlerMiddleware>()
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
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ResponsePublished));

		public static Action<IPipeBuilder> RespondPipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.Initiated))
			.Use<RespondConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumeConfigured))
			.Use<QueueDeclareMiddleware>(new QueueDeclareOptions { QueueFunc = context => context.GetRespondConfiguration()?.Queue})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.QueueDeclared))
			.Use<ExchangeDeclareMiddleware>(new ExchangeDeclareOptions { ExchangeFunc = context => context.GetRespondConfiguration()?.Exchange})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ExchangeDeclared))
			.Use<QueueBindMiddleware>(new QueueBindOptions { ConsumeFunc = context => context.GetRespondConfiguration() })
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.QueueBound))
			.Use<ConsumerCreationMiddleware>(new ConsumerCreationOptions { ConsumeFunc = context => context.GetRespondConfiguration()})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.ConsumerCreated))
			.Use<MessageConsumeMiddleware>(new ConsumeOptions { Pipe = ConsumePipe })
			.Use<SubscriptionMiddleware>(new SubscriptionOptions { QueueFunc = context => context.GetRespondConfiguration()?.Queue});

		public static Task RespondAsync<TRequest, TResponse>(this IBusClient client, Func<TRequest, Task<TResponse>> handler, Action<IRespondConfigurationBuilder> configuration = null)
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
