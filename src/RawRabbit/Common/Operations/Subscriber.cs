using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Operations
{
	public interface ISubscriber
	{
		Task SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, SubscriptionConfiguration config) where T : MessageBase;
	}

	public class Subscriber : OperatorBase, ISubscriber
	{
		public Subscriber(IChannelFactory channelFactory, IMessageSerializer serializer)
			: base(channelFactory, serializer)
		{
		}

		public Task SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, SubscriptionConfiguration config) where T : MessageBase
		{
			var queueTask = DeclareQueueAsync(config.Queue);
			var exchangeTask = DeclareExchangeAsync(config.Exchange);
			
			return Task
				.WhenAll(queueTask, exchangeTask)
				.ContinueWith(t => BindQueue(config.Queue, config.Exchange))
				.ContinueWith(t => SubscribeAsync<T>(config, subscribeMethod));
		}

		private Task SubscribeAsync<T>(SubscriptionConfiguration config, Func<T, MessageInformation, Task> subscribeMethod)
		{
			return Task.Run(() =>
			{
				var channel = ChannelFactory.GetChannel();
				ConfigureQosAsync(channel, config.PrefetchCount);
				var consumer = new EventingBasicConsumer(channel);
				consumer.Received += (model, ea) =>
				{
					Task
						.Run(() => Serializer.Deserialize<T>(ea.Body))
						.ContinueWith(serializeTask =>
							{
								return Task
									.Run(() => subscribeMethod(serializeTask.Result, null))
									.ContinueWith(subscribeTask => BasicAck(channel, ea.DeliveryTag));
							}
						);
				};

				channel.BasicConsume(
					queue: config.Queue.QueueName,
					noAck: config.NoAck,
					consumer: consumer
				);
			});
		}
	}
}
