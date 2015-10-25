using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Contract;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Subscriber<TMessageContext> : OperatorBase, ISubscriber<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public Subscriber(IChannelFactory channelFactory, IConsumerFactory consumerFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
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

		private Task SubscribeAsync<T>(SubscriptionConfiguration cfg, Func<T, TMessageContext, Task> subscribeMethod)
		{
			return Task.Run(() =>
			{
				var consumer = _consumerFactory.CreateConsumer(cfg);
				consumer.OnMessageAsync = (o, args) =>
				{
					var bodyTask = Task.Run(() => Serializer.Deserialize<T>(args.Body));
					var contextTask = _contextProvider.ExtractContextAsync(args.BasicProperties.Headers[_contextProvider.ContextHeaderName]);
					return Task
						.WhenAll(bodyTask, contextTask)
						.ContinueWith(task => subscribeMethod(bodyTask.Result, contextTask.Result));
				};

				consumer.Model.BasicConsume(
					queue: cfg.Queue.QueueName,
					noAck: cfg.NoAck,
					consumer: consumer
				);
			});
		}
	}
}
