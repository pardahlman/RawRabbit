using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publish;
using RawRabbit.Operations.Publish;
using RawRabbit.Operations.Publish.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class PublishMessageExtension
	{
		private static readonly Action<IPipeBuilder> PublishPipeAction = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.Initiated))
			.Use<PublishConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ExchangeConfigured))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ExchangeDeclared))
			.Use<RoutingKeyMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.RoutingKeyCreated))
			.Use<MessageSerializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.MessageSerialized))
			.Use<BasicPropertiesMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.BasicPropertiesCreated))
			.Use<TransientChannelMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ChannelCreated))
			.Use<MandatoryCallbackMiddleware>()
			.Use<PublishAcknowledgeMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.PreMessagePublish))
			.Use<PublishMessage>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.MessagePublished));

		public static Task PublishAsync<TMessage>(this IBusClient client, TMessage message, Action<IPublishConfigurationBuilder> config = null)
		{
			return client.InvokeAsync(
				PublishPipeAction,
				ctx =>
				{
					ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.Add(PipeKey.Message, message);
					ctx.Properties.Add(PipeKey.ConfigurationAction, config);
				});
		}
	}
}
