﻿using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Extensions.MessageSequence.Core.Abstraction;
using RawRabbit.Serialization;

namespace RawRabbit.Extensions.MessageSequence.Core
{
	public class MessageChainTopologyUtil<TMessageContext> : IMessageChainTopologyUtil where TMessageContext : IMessageContext
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ITopologyProvider _topologyProvider;
		private readonly IConfigurationEvaluator _configEvaluator;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IMessageChainDispatcher _messageDispatcher;
		private readonly QueueConfiguration _queueConfig;
		private EventingBasicConsumer _consumer;

		public MessageChainTopologyUtil(
			IChannelFactory channelFactory,
			ITopologyProvider topologyProvider,
			IConfigurationEvaluator configEvaluator,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IMessageChainDispatcher messageDispatcher,
			QueueConfiguration queueConfig)
		{
			_channelFactory = channelFactory;
			_topologyProvider = topologyProvider;
			_configEvaluator = configEvaluator;
			_queueConfig = queueConfig;
			_messageDispatcher = messageDispatcher;
			_contextProvider = contextProvider;
			_serializer = serializer;
			InitializeConsumer();
		}

		public Task BindToExchange<TMessage>(Guid globalMessageId)
		{
			return BindToExchange(typeof(TMessage), globalMessageId);
		}

		public Task BindToExchange(Type messageType, Guid globalMessageId)
		{
			var chainConfig = _configEvaluator.GetConfiguration(messageType);
			return _topologyProvider.BindQueueAsync(
				_queueConfig,
				chainConfig.Exchange,
				$"{chainConfig.RoutingKey}.{globalMessageId}"
			);
		}

		public Task UnbindFromExchange<TMessage>(Guid globalMessageId)
		{
			return UnbindFromExchange(typeof(TMessage), globalMessageId);
		}

		public Task UnbindFromExchange(Type messageType, Guid globalMessageId)
		{
			var chainConfig = _configEvaluator.GetConfiguration(messageType);
			return _topologyProvider.UnbindQueueAsync(
				_queueConfig,
				chainConfig.Exchange,
				$"{chainConfig.RoutingKey}.{globalMessageId}"
			);
		}

		private void InitializeConsumer()
		{
			var channelTask = _channelFactory.CreateChannelAsync();
			var topologyTask = _topologyProvider.DeclareQueueAsync(_queueConfig);

			Task.WhenAll(channelTask, topologyTask)
				.ContinueWith(tChannel =>
					{
						_consumer = new EventingBasicConsumer(channelTask.Result);
						_consumer.Received += (sender, args) =>
						{
							var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[PropertyHeaders.Context]);
							var body = _serializer.Deserialize(args);
							_messageDispatcher.InvokeMessageHandler(context.GlobalRequestId, body, context);
							_consumer.Model.BasicAck(args.DeliveryTag, false);
						};
						_consumer.Model.BasicConsume(_queueConfig.FullQueueName, false, _consumer);
					})
				.Wait();
		}
	}
}
