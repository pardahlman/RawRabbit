using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Message;

namespace RawRabbit.Common
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
			var basicQosTask = ConfigureQosAsync(config);
			
			return Task
				.WhenAll(queueTask, exchangeTask, basicQosTask)
				.ContinueWith(t => BindQueueAsync(config))
				.ContinueWith(t => SubscribeAsync<T>(config, subscribeMethod));
		}

		private Task SubscribeAsync<T>(SubscriptionConfiguration config, Func<T, MessageInformation, Task> subscribeMethod)
		{
			return Task.Factory.StartNew(() =>
			{
				var channel = _channelFactory.GetChannel();
				var consumer = new EventingBasicConsumer(channel);
				consumer.Received += (model, ea) =>
				{
					Task.Factory
						.StartNew(() => _serializer.Deserialize<T>(ea.Body))
						.ContinueWith(t =>
							{
								var subscribeTask = Task.Factory.StartNew(() => subscribeMethod(t.Result, null));
								var ackTask = BasicAckAsync(ea.DeliveryTag);
								Task.WhenAll(subscribeTask, ackTask);
							});
				};

				channel.BasicConsume(
					queue: config.Queue.QueueName,
					noAck: config.NoAck,
					consumer: consumer
				);
			});
		}

		private Task BasicAckAsync(ulong deliveryTag)
		{
			return Task.Factory.StartNew(() =>
				_channelFactory
					.GetChannel()
					.BasicAck(
					deliveryTag: deliveryTag,
					multiple: false
				)
			);
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

		private Task ConfigureQosAsync(SubscriptionConfiguration config)
		{
			return Task.Factory.StartNew(() =>
			{
				_channelFactory
					.GetChannel()
					.BasicQos(
						prefetchSize: 0, //TODO : what is this?
						prefetchCount: config.PrefetchCount,
						global: false // https://www.rabbitmq.com/consumer-prefetch.html
				);
			});
		}
	}
}
