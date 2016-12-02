using System;
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
		private readonly QueueDeclaration _queueConfig;
		private EventingBasicConsumer _consumer;

		public MessageChainTopologyUtil(
			IChannelFactory channelFactory,
			ITopologyProvider topologyProvider,
			IConfigurationEvaluator configEvaluator,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IMessageChainDispatcher messageDispatcher,
			QueueDeclaration queueConfig)
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

		public Task BindToExchange<TMessage>(Guid globalMessaegId)
		{
			return BindToExchange(typeof(TMessage), globalMessaegId);
		}

		public Task BindToExchange(Type messageType, Guid globalMessaegId)
		{
			var chainConfig = _configEvaluator.GetConfiguration(messageType);
			return _topologyProvider.BindQueueAsync(
				_queueConfig.Name,
				chainConfig.Exchange.Name,
				$"{chainConfig.RoutingKey}.{globalMessaegId}"
			);
		}

		public Task UnbindFromExchange<TMessage>(Guid globalMessaegId)
		{
			return UnbindFromExchange(typeof(TMessage), globalMessaegId);
		}

		public Task UnbindFromExchange(Type messageType, Guid globalMessaegId)
		{
			var chainConfig = _configEvaluator.GetConfiguration(messageType);
			return _topologyProvider.UnbindQueueAsync(
				_queueConfig.Name,
				chainConfig.Exchange.Name,
				$"{chainConfig.RoutingKey}.{globalMessaegId}"
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
