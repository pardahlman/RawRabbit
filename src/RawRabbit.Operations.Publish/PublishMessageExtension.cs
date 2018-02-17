using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Operations.Publish;
using RawRabbit.Operations.Publish.Context;
using RawRabbit.Operations.Publish.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class PublishMessageExtension
	{
		public static readonly Action<IPipeBuilder> PublishPipeAction = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.Initialized))
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.ProducerInitialized))
			.Use<PublishConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.PublishConfigured))
			.Use<ExchangeDeclareMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ExchangeDeclared))
			.Use<BodySerializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.MessageSerialized))
			.Use<BasicPropertiesMiddleware>(new BasicPropertiesOptions { PostCreateAction = (ctx, props) =>
			{
				props.Headers.TryAdd(PropertyHeaders.Sent, DateTime.UtcNow.ToString("O"));
			}})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(StageMarker.BasicPropertiesCreated))
			.Use<PooledChannelMiddleware>(new PooledChannelOptions{PoolNameFunc = c => PublishKey.Publish})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.ChannelCreated))
			.Use<ReturnCallbackMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.PreMessagePublish))
			.Use<PublishAcknowledgeMiddleware>()
			.Use<BasicPublishMiddleware>(new BasicPublishOptions
			{
				BodyFunc = c => c.Get<byte[]>(PipeKey.SerializedMessage)
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(PublishStage.MessagePublished));

		public static Task PublishAsync<TMessage>(this IBusClient client, TMessage message, Action<IPublishContext> context = null, CancellationToken token = default(CancellationToken))
		{
			return client.InvokeAsync(
				PublishPipeAction,
				ctx =>
				{
					ctx.Properties.Add(PipeKey.MessageType, message?.GetType() ?? typeof(TMessage));
					ctx.Properties.Add(PipeKey.Message, message);
					context?.Invoke(new PublishContext(ctx));
				}, token);
		}
	}
}
