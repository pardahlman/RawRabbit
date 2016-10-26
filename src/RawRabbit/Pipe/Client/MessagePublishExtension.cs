using System;
using System.Text;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publish;
using RawRabbit.Context;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe.Client
{
	public static class MessagePublishExtension
	{
		private static readonly Action<IPipeBuilder> PublishPipeAction = pipe => pipe

			.Use<OperationConfigurationMiddleware>()
			.Use<MessageChainingMiddleware>()
			.Use<GlobalMessageIdMiddleware>()
			.Use<RoutingKeyMiddleware>()
			.Use<ExchangeDeclareMiddleware>()
			.Use<MessageContextCreationMiddleware<MessageContext>>()
			.Use<ContextSerializationMiddleware>()
			.Use<BasicPropertiesMiddleware>()
			.Use<MessageSerializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.PreChannelCreation))
			.Use<PublishChannelMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.PostChannelCreation))
			.Use<PublishAcknowledgeMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.PreMessagePublish))
			.Use<PublishMessage>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.PostMessagePublish));

		public static Task PublishAsync<TMessage>(this IBusClient client, TMessage message, Action<IPublishConfigurationBuilder> config = null)
		{
			return client.InvokeAsync(
				PublishPipeAction,
				ctx =>
				{
					ctx.Properties.Add(PipeKey.Operation, Operation.Publish);
					ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.Add(PipeKey.Message, message);
					ctx.Properties.Add(PipeKey.ConfigurationAction, config);
				});
		}
	}
}