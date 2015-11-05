using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Contract;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Logging;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Subscriber<TMessageContext> : OperatorBase, ISubscriber<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly ILogger _logger = LogManager.GetLogger<Subscriber<TMessageContext>>();

		public Subscriber(IChannelFactory channelFactory, IConsumerFactory consumerFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
			_contextProvider = contextProvider;
		}

		public Task SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, SubscriptionConfiguration config)
		{
			DeclareQueue(config.Queue);
			DeclareExchange(config.Exchange);
			BindQueue(config.Queue, config.Exchange, config.RoutingKey);
			SubscribeAsync(config, subscribeMethod);
			return Task.FromResult(true);
		}

		private void SubscribeAsync<T>(SubscriptionConfiguration cfg, Func<T, TMessageContext, Task> subscribeMethod)
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

			_logger.LogDebug($"Setting up a consumer on queue {cfg.Queue.QueueName} with NoAck set to {cfg.NoAck}.");
			consumer.Model.BasicConsume(
				queue: cfg.Queue.QueueName,
				noAck: cfg.NoAck,
				consumer: consumer
			);
		}
	}
}
