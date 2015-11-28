using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Contract;
using RawRabbit.Context;
using RawRabbit.Context.Enhancer;
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
			var consumer = _consumerFactory.CreateConsumer(cfg);
			consumer.OnMessageAsync = (o, args) =>
			{
				var bodyTask = Task.Run(() => Serializer.Deserialize<T>(args.Body));
				var contextTask = _contextProvider
					.ExtractContextAsync(args.BasicProperties.Headers[_contextProvider.ContextHeaderName])
					.ContinueWith(ctxTask =>
					{
						_contextEnhancer.WireUpContextFeatures(ctxTask.Result, consumer, args);
						return ctxTask.Result;
					});
				return Task
					.WhenAll(bodyTask, contextTask)
					.ContinueWith(task => subscribeMethod(bodyTask.Result, contextTask.Result));
			};
			consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);

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
