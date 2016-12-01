using System;
using System.Text;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Operations.Publish;
using RawRabbit.Operations.Publish.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class PublishMessageExtension
	{
		public static readonly Action<IPipeBuilder> PublishPipeAction = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.Initiated))
			.Use<PublisherConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.PublishConfigured))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ExchangeDeclared))
			.Use<BodySerializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.MessageSerialized))
			.Use<BasicPropertiesMiddleware>(new BasicPropertiesOptions { PostCreateAction = (ctx, props) =>
			{
				props.Headers.TryAdd(PropertyHeaders.Sent, DateTime.UtcNow.ToString("u"));
				props.Headers.TryAdd(PropertyHeaders.MessageType, ctx.GetMessageType().GetUserFriendlyName());
			}})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.BasicPropertiesCreated))
			.Use<TransientChannelMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ChannelCreated))
			.Use<MandatoryCallbackMiddleware>()
			.Use<PublishAcknowledgeMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.PreMessagePublish))
			.Use<BasicPublishMiddleware>(new BasicPublishOptions
			{
				BodyFunc = c => UTF8Encoding.UTF8.GetBytes(c.Get<string>(PipeKey.SerializedMessage))
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.MessagePublished));

		public static Task PublishAsync<TMessage>(this IBusClient client, TMessage message, Action<IPublisherConfigurationBuilder> config = null)
		{
			return client.InvokeAsync(
				PublishPipeAction,
				ctx =>
				{
					ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.Add(PipeKey.Message, message);
					ctx.Properties.Add(PipeKey.Operation, PublishKey.Publish);
					ctx.Properties.Add(PipeKey.ConfigurationAction, config);
				});
		}
	}
}
