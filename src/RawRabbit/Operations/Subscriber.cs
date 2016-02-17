using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context;
using RawRabbit.Context.Enhancer;
using RawRabbit.Context.Provider;
using RawRabbit.Logging;
using RawRabbit.Operations.Abstraction;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Subscriber<TMessageContext> : OperatorBase, ISubscriber<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IContextEnhancer _contextEnhancer;
		private readonly ILogger _logger = LogManager.GetLogger<Subscriber<TMessageContext>>();

		public Subscriber(IChannelFactory channelFactory, IConsumerFactory consumerFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider, IContextEnhancer contextEnhancer)
			: base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
			_contextProvider = contextProvider;
			_contextEnhancer = contextEnhancer;
		}

		public void SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, SubscriptionConfiguration config)
		{
			DeclareQueue(config.Queue);
			DeclareExchange(config.Exchange);
			BindQueue(config.Queue, config.Exchange, config.RoutingKey);
			SubscribeAsync(config, subscribeMethod);
		}

		private void SubscribeAsync<T>(SubscriptionConfiguration cfg, Func<T, TMessageContext, Task> subscribeMethod)
		{
			var consumer = _consumerFactory.CreateConsumer(cfg, ChannelFactory.CreateChannel());
			consumer.OnMessageAsync = (o, args) =>
			{
				var body = Serializer.Deserialize<T>(args.Body);
				var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[PropertyHeaders.Context]);
				_contextEnhancer.WireUpContextFeatures(context, consumer, args);
				return subscribeMethod(body, context);
			};
			consumer.Model.BasicConsume(cfg.Queue.FullQueueName, cfg.NoAck, consumer);

			_logger.LogDebug($"Setting up a consumer on queue {cfg.Queue.QueueName} with NoAck set to {cfg.NoAck}.");
		}

		public override void Dispose()
		{
			_logger.LogDebug("Disposing Subscriber.");
			base.Dispose();
			(_consumerFactory as IDisposable)?.Dispose();
		}
	}
}
