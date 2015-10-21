using System;
using System.CodeDom;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Exchange;
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
		private readonly IChannelFactory _channelFactory;
		private readonly IMessageSerializer _serializer;

		public Subscriber(IChannelFactory channelFactory, IMessageSerializer serializer)
			: base(channelFactory)
		{
			_channelFactory = channelFactory;
			_serializer = serializer;
		}

		public Task SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, SubscriptionConfiguration config) where T : MessageBase
		{
			var queueTask = DeclareQueueAsync(config.Queue);
			var exchangeTask = DeclareExchangeAsync(config.Exchange);
			
			return Task
				.WhenAll(queueTask, exchangeTask)
				.ContinueWith(t => BindQueueAsync(config))
				.ContinueWith(t => SubscribeAsync<T>(config, subscribeMethod));
		}

		private Task SubscribeAsync<T>(SubscriptionConfiguration config, Func<T, MessageInformation, Task> subscribeMethod)
		{
			return Task.Factory.StartNew(() =>
			{
				var channel = _channelFactory.GetChannel();
				ConfigureQosAsync(channel, config.PrefetchCount);
				var consumer = new EventingBasicConsumer(channel);
				consumer.Received += (model, ea) =>
				{
					Task.Factory
						.StartNew(() => _serializer.Deserialize<T>(ea.Body))
						.ContinueWith(serializeTask =>
							{
								return Task.Factory
									.StartNew(() => subscribeMethod(serializeTask.Result, null))
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

		private Task BindQueueAsync(SubscriptionConfiguration config)
		{
			if (config.Exchange.IsDefaultExchange())
			{
				return Task.FromResult(true);
			}
			return Task.Factory.StartNew(() =>
			{
				_channelFactory
					.GetChannel()
					.QueueBind(
						queue: config.Queue.QueueName,
						exchange: config.Exchange.ExchangeName,
						routingKey: config.RoutingKey
				);
			});
		}
	}
}
