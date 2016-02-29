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
			var channel = ChannelFactory.CreateChannel();
			DeclareQueue(config.Queue, channel);
			DeclareExchange(config.Exchange, channel);
			BindQueue(config.Queue, config.Exchange, config.RoutingKey, channel);
			var consumer = _consumerFactory.CreateConsumer(config, channel);
			consumer.OnMessageAsync = (o, args) =>
			{
				var body = Serializer.Deserialize<T>(args.Body);
				var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[PropertyHeaders.Context]);
				_contextEnhancer.WireUpContextFeatures(context, consumer, args);
				return subscribeMethod(body, context);
			};
			consumer.Model.BasicConsume(config.Queue.FullQueueName, config.NoAck, consumer);

			_logger.LogDebug($"Setting up a consumer on queue {config.Queue.QueueName} with NoAck set to {config.NoAck}.");
		}

		public override void Dispose()
		{
			_logger.LogDebug("Disposing Subscriber.");
			base.Dispose();
			(_consumerFactory as IDisposable)?.Dispose();
		}
	}
}
