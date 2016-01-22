using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Logging;
using RawRabbit.Operations.Abstraction;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Publisher<TMessageContext> : OperatorBase, IPublisher where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IPublishAcknowledger _acknowledger;
		private readonly IBasicPropertiesProvider _propertiesProvider;
		protected IModel _channel;
		private Timer _channelTimer;
		private readonly ILogger _logger = LogManager.GetLogger<Publisher<TMessageContext>>();

		public Publisher(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider, IPublishAcknowledger acknowledger, IBasicPropertiesProvider propertiesProvider)
			: base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
			_acknowledger = acknowledger;
			_propertiesProvider = propertiesProvider;
		}

		public Task PublishAsync<TMessage>(TMessage message, Guid globalMessageId, PublishConfiguration config)
		{
			var context = _contextProvider.GetMessageContext(globalMessageId);
			var channel = GetOrCreateChannel(config);
			var props = _propertiesProvider.GetProperties<TMessage>(config.PropertyModifier + (p => p.Headers.Add(PropertyHeaders.Context, context)));

			var publishAckTask = _acknowledger.GetAckTask();
			channel.BasicPublish(
				exchange: config.Exchange.ExchangeName,
				routingKey: config.RoutingKey,
				basicProperties: props,
				body: Serializer.Serialize(message)
				);
			return publishAckTask;
		}

		protected virtual IModel GetOrCreateChannel(PublishConfiguration config)
		{
			if (_channel?.IsOpen ?? false)
			{
				return _channel;
			}
			_channelTimer = new Timer(state =>
			{
				var firstRef = _channel.NextPublishSeqNo;
				Timer inner = null;
				inner = new Timer(o =>
				{
					inner?.Dispose();
					var secondRef = _channel.NextPublishSeqNo;
					if (firstRef != secondRef)
					{
						_logger.LogDebug($"Channel has published ${secondRef-firstRef} message(s) the last second and is still considered active.");
						return;
					}
					_logger.LogInformation($"sClosing publish channel '{_channel.ChannelNumber}'");
					_channelTimer?.Dispose();
					_channel.Dispose();
				}, null, TimeSpan.FromSeconds(1), new TimeSpan(-1));
			}, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

			_channel = ChannelFactory.CreateChannel();
			_acknowledger.SetActiveChannel(_channel);
			DeclareExchange(config.Exchange, _channel);

			return _channel;
		}

		public override void Dispose()
		{
			_logger.LogDebug("Disposing Publisher");
			_channelTimer?.Dispose();
			_channel?.Dispose();
			base.Dispose();
		}
	}
}
