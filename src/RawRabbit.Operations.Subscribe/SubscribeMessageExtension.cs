using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Respond;
using RawRabbit.Operations.Subscribe.Middleware;
using RawRabbit.Operations.Subscribe.Stages;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class SubscribeMessageExtension
	{
		private static readonly Action<IPipeBuilder> ConsumePipe = pipe => pipe
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.MessageRecieved))
			.Use<MessageDeserializationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.MessageDeserialized))
			.Use<AutoAckMessageHandlerMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(ConsumerStage.HandlerInvoked));

		public static readonly Action<IPipeBuilder> SubscribePipe = pipe => pipe
			.Use<ConsumeConfigurationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConfigurationCreated))
			.Use<QueueDeclareMiddleware>(new QueueDeclareOptions { QueueFunc = context => context.GetConsumerConfiguration()?.Queue})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.QueueDeclared))
			.Use<ExchangeDeclareMiddleware>(new ExchangeDeclareOptions { ExchangeFunc = context => context.GetConsumerConfiguration()?.Exchange})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ExchangeDeclared))
			.Use<QueueBindMiddleware>(new QueueBindOptions {ConsumeFunc = context => context.GetConsumerConfiguration()})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.QueueBound))
			.Use<ConsumerCreationMiddleware>()
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumerChannelCreated))
			.Use<MessageConsumeMiddleware>(new ConsumeOptions
			{
				Pipe = ConsumePipe
			})
			.Use<StageMarkerMiddleware>(StageMarkerOptions.For(SubscribeStage.ConsumerCreated))
			.Use<SubscriptionMiddleware>();

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task> subscribeMethod, Action<IConsumeConfigurationBuilder> configuration = null)
		{
			return client.InvokeAsync(
				SubscribePipe,
				context =>
				{
					Func<object, Task> genericHandler = o => subscribeMethod((TMessage) o);

					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					context.Properties.Add(PipeKey.MessageHandler, genericHandler);
					context.Properties.Add(PipeKey.ConfigurationAction, configuration);
				}
			);
		}

		public static Task SubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task<Acknowledgement>> subscribeMethod, Action<IConsumeConfigurationBuilder> configuration = null)
		{
			return client.InvokeAsync(
				SubscribePipe + (pipe => pipe
					.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
					{
						Pipe = ConsumePipe + (builder => builder.Replace<AutoAckMessageHandlerMiddleware, ExplicitAckMessageHandlerMiddleware>())
					})),
				context =>
				{
					Func<object, Task> genericHandler = o => subscribeMethod((TMessage) o);

					context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					context.Properties.Add(PipeKey.MessageHandler, genericHandler);
					context.Properties.Add(PipeKey.ConfigurationAction, configuration);
				}
			);
		}
	}
}	
