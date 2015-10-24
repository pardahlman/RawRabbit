using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public interface ISubscriber<out TMessageContext> where TMessageContext : IMessageContext
	{
		Task SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, SubscriptionConfiguration config);
	}

	public class Subscriber<TMessageContext> : OperatorBase, ISubscriber<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public Subscriber(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
		}

		public Task SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, SubscriptionConfiguration config)
		{
			var queueTask = DeclareQueueAsync(config.Queue);
			var exchangeTask = DeclareExchangeAsync(config.Exchange);
			
			return Task
				.WhenAll(queueTask, exchangeTask)
				.ContinueWith(t => BindQueue(config.Queue, config.Exchange, config.RoutingKey))
				.ContinueWith(t => SubscribeAsync<T>(config, subscribeMethod));
		}

		private Task SubscribeAsync<T>(SubscriptionConfiguration config, Func<T, TMessageContext, Task> subscribeMethod)
		{
			return Task.Run(() =>
			{
				var channel = ChannelFactory.GetChannel();
				ConfigureQosAsync(channel, config.PrefetchCount);
				var consumer = new EventingBasicConsumer(channel);
				consumer.Received += (model, ea) =>
				{
					var bodyTask = Task.Run(() => Serializer.Deserialize<T>(ea.Body));
					var contextTask = _contextProvider.ExtractContextAsync(ea.BasicProperties.Headers[_contextProvider.ContextHeaderName]);
					Task
						.WhenAll(bodyTask, contextTask)
						.ContinueWith(task =>
						{
							Task subscribeTask;
							try
							{
								subscribeTask = subscribeMethod(bodyTask.Result, contextTask.Result);
							}
							catch (Exception)
							{
								return;
								// TODO: error handling here.
							}
							subscribeTask
								.ContinueWith(t=> BasicAck(channel, ea.DeliveryTag));
						});
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
