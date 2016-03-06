using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context;
using RawRabbit.Context.Enhancer;
using RawRabbit.Context.Provider;
using RawRabbit.Extensions.MessageSequence.Core.Abstraction;
using RawRabbit.Serialization;

namespace RawRabbit.Extensions.MessageSequence.Core
{
	public class MessageChainTopologyUtil<TMessageContext> : IMessageChainTopologyUtil where TMessageContext : IMessageContext
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IConsumerFactory _consumerFactory;
		private readonly ITopologyProvider _topologyProvider;
		private readonly IConfigurationEvaluator _configEvaluator;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IContextEnhancer _contextEnhancer;
		private readonly IMessageChainDispatcher _messageDispatcher;
		private readonly ConcurrentDictionary<Guid, object> _msgIdDictionary;
		private readonly QueueConfiguration _queueConfig;
		private readonly List<Type> _boundExchanges;
		private readonly Task _completed = Task.FromResult(true);
		private bool _isInitialized;
		private readonly object _padlock = new object();

		public MessageChainTopologyUtil(
			IChannelFactory channelFactory,
			IConsumerFactory consumerFactory,
			ITopologyProvider topologyProvider,
			IConfigurationEvaluator configEvaluator,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IContextEnhancer contextEnhancer,
			IMessageChainDispatcher messageDispatcher,
			QueueConfiguration queueConfig)
		{
			_channelFactory = channelFactory;
			_consumerFactory = consumerFactory;
			_topologyProvider = topologyProvider;
			_configEvaluator = configEvaluator;
			_queueConfig = queueConfig;
			_contextEnhancer = contextEnhancer;
			_messageDispatcher = messageDispatcher;
			_contextProvider = contextProvider;
			_serializer = serializer;
			_boundExchanges = new List<Type>();
			_msgIdDictionary = new ConcurrentDictionary<Guid, object>();
		}

		public Task BindToExchange<TMessage>()
		{
			var chainConfig = _configEvaluator.GetConfiguration<TMessage>();
			if (_boundExchanges.Contains(typeof(TMessage)))
			{
				return _completed;
			}
			_boundExchanges.Add(typeof(TMessage));
			InitializeConsumer();
			return _topologyProvider.BindQueueAsync(_queueConfig, chainConfig.Exchange, chainConfig.RoutingKey);
		}

		public void Unregister(Guid globalRequestId)
		{
			object removedLock;
			_msgIdDictionary.TryRemove(globalRequestId, out removedLock);
		}

		private void InitializeConsumer()
		{
			lock (_padlock)
			{
				if (_isInitialized)
				{
					return;
				}
				var channelTask = _channelFactory.CreateChannelAsync();
				var topologyTask = _topologyProvider.DeclareQueueAsync(_queueConfig);

				Task.WhenAll(channelTask, topologyTask)
					.ContinueWith(tChannel =>
						{
							var consumer = _consumerFactory.CreateConsumer(new SubscriptionConfiguration { NoAck = false, PrefetchCount = 50 }, channelTask.Result);
							consumer.OnMessageAsync = (o, args) =>
							{
								var body = _serializer.Deserialize(args);
								var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[PropertyHeaders.Context]);
								_contextEnhancer.WireUpContextFeatures(context, consumer, args);
								object msgLock;
								if (!_msgIdDictionary.TryGetValue(context.GlobalRequestId, out msgLock))
								{
									msgLock = new object();
									if (!_msgIdDictionary.TryAdd(context.GlobalRequestId, msgLock))
									{
										_msgIdDictionary.TryGetValue(context.GlobalRequestId, out msgLock);
									}
								};
								return Task.Run(() =>
								{
									lock (msgLock)
									{
										_messageDispatcher
											.InvokeMessageHandlerAsync(context.GlobalRequestId, body, context)
											.Wait();
									}
								});
							};
							consumer.Model.BasicConsume(_queueConfig.FullQueueName, false, consumer);
						})
					.Wait();
				_isInitialized = true;
			}
		}
	}
}
