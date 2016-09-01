using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Publish;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Logging;
using RawRabbit.Operations.Abstraction;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Publisher<TMessageContext> : IPublisher where TMessageContext : IMessageContext
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ITopologyProvider _topologyProvider;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IPublishAcknowledger _acknowledger;
		private readonly IBasicPropertiesProvider _propertiesProvider;
		private readonly RawRabbitConfiguration _config;
		private readonly ILogger _logger = LogManager.GetLogger<Publisher<TMessageContext>>();
		private readonly object _publishLock = new object();
		private readonly object _topologyLock = new object();

		public Publisher(IChannelFactory channelFactory, ITopologyProvider topologyProvider, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider, IPublishAcknowledger acknowledger, IBasicPropertiesProvider propertiesProvider, RawRabbitConfiguration config)
		{
			_channelFactory = channelFactory;
			_topologyProvider = topologyProvider;
			_serializer = serializer;
			_contextProvider = contextProvider;
			_acknowledger = acknowledger;
			_propertiesProvider = propertiesProvider;
			_config = config;
		}

		public Task PublishAsync<TMessage>(TMessage message, Guid globalMessageId, PublishConfiguration config)
		{
			var context = _contextProvider.GetMessageContext(out globalMessageId);
			var props = _propertiesProvider.GetProperties<TMessage>(config.PropertyModifier + (p => p.Headers.Add(PropertyHeaders.Context, context)));

			Task exchangeTask;
			lock (_topologyLock)
			{
				exchangeTask = _topologyProvider.DeclareExchangeAsync(config.Exchange);
			}
			var channelTask = _channelFactory.GetChannelAsync();

			return Task
				.WhenAll(exchangeTask, channelTask)
				.ContinueWith(t =>
					{
						lock (_publishLock)
						{
							var ackTask = _acknowledger.GetAckTask(channelTask.Result);
							channelTask.Result.BasicPublish(
								exchange: config.Exchange.ExchangeName,
								routingKey: _config.RouteWithGlobalId ? $"{config.RoutingKey}.{globalMessageId}" : config.RoutingKey,
								basicProperties: props,
								body: _serializer.Serialize(message)
								);
							return ackTask;
						}
					})
				.Unwrap();
		}
	}
}
