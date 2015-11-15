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
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Publisher<TMessageContext> : OperatorBase, IPublisher where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly ThreadLocal<Timer> _timer; 

		public Publisher(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
			_timer = new ThreadLocal<Timer>();
		}

		public Task PublishAsync<T>(T message, Guid globalMessageId, PublishConfiguration config)
		{
			return _contextProvider
				.GetMessageContextAsync(globalMessageId)
				.ContinueWith(ctxTask =>
				{
					var channel = GetChannel();
					DeclareQueue(config.Queue, channel);
					DeclareExchange(config.Exchange, channel);
					var properties = new BasicProperties
					{
						MessageId = Guid.NewGuid().ToString(),
						Headers = new Dictionary<string, object>
						{
							[_contextProvider.ContextHeaderName] = ctxTask.Result
						}
					};

					channel.BasicPublish(
						exchange: config.Exchange.ExchangeName,
						routingKey: config.RoutingKey,
						basicProperties: properties,
						body: Serializer.Serialize(message)
						);
					channel.Dispose();
				});
		}

		private IModel GetChannel()
		{
			if (_timer.IsValueCreated)
			{
				return ChannelFactory.GetChannel();
			}

			var channel = ChannelFactory.GetChannel();

			Timer closeChannel = null;
			closeChannel = new Timer(state =>
			{
				closeChannel?.Dispose();
				channel.Dispose();
			}, null, TimeSpan.FromSeconds(1), new TimeSpan(-1));
			_timer.Value = closeChannel;
			return channel;
		}

		public override void Dispose()
		{
			base.Dispose();
			_timer.Dispose();
		}
	}
}
