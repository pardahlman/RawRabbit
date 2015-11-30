using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Logging;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Publisher<TMessageContext> : OperatorBase, IPublisher where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private ThreadLocal<IModel> _channel;
		private Timer _channelTimer;
		private readonly ILogger _logger = LogManager.GetLogger<Publisher<TMessageContext>>();

		public Publisher(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
		}

		public Task PublishAsync<TMessage>(TMessage message, Guid globalMessageId, PublishConfiguration config)
		{
			return Task.Run(() =>
			{
				var context = _contextProvider.GetMessageContext(globalMessageId);
				var channel = GetOrCreateChannel();
				DeclareQueue(config.Queue, channel);
				DeclareExchange(config.Exchange, channel);
				var properties = new BasicProperties
				{
					MessageId = Guid.NewGuid().ToString(),
					Headers = new Dictionary<string, object>
						{
							{ _contextProvider.ContextHeaderName, context },
							{PropertyHeaders.Sent, DateTime.UtcNow.ToString("u") }
						}
				};

				channel.BasicPublish(
					exchange: config.Exchange.ExchangeName,
					routingKey: config.RoutingKey,
					basicProperties: properties,
					body: Serializer.Serialize(message)
					);
			});
		}

		private IModel GetOrCreateChannel()
		{
			if (_channel == null)
			{
				_channel = new ThreadLocal<IModel>(() => ChannelFactory.CreateChannel());
				_channelTimer = new Timer(state =>
				{
					foreach (var channel in _channel.Values)
					{
						channel?.Dispose();
					}
					_channel?.Dispose();
					_channel = null;
				}, null, TimeSpan.FromSeconds(1), new TimeSpan(-1));
			}
			if (_channel.Value.IsClosed)
			{
				_channel.Value.Dispose();
				_channel.Value = ChannelFactory.CreateChannel();
			}
			_channelTimer.Change(TimeSpan.FromSeconds(1), new TimeSpan(-1));
			return _channel.Value;
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
