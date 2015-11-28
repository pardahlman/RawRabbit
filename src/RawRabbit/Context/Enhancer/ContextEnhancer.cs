using System;
using System.Collections.Generic;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Consumer.Contract;

namespace RawRabbit.Context.Enhancer
{
	public class ContextEnhancer : IContextEnhancer
	{
		private readonly IChannelFactory _channelFactory;
		private readonly INamingConvetions _convetions;

		public ContextEnhancer(IChannelFactory channelFactory, INamingConvetions convetions)
		{
			_channelFactory = channelFactory;
			_convetions = convetions;
		}

		public void WireUpContextFeatures<TMessageContext>(TMessageContext context, IRawConsumer consumer, BasicDeliverEventArgs args) where TMessageContext : IMessageContext
		{
			if (context == null)
			{
				return;
			}

			var advancedCtx = context as IAdvancedMessageContext;
			if (advancedCtx != null)
			{
				advancedCtx.Nack = () =>
				{
					consumer.NackedDeliveryTags.Add(args.DeliveryTag);
					consumer.Model.BasicNack(args.DeliveryTag, false, true);
				};

				advancedCtx.RetryLater = timespan =>
				{
					var dlxName = _convetions.DeadLetterExchangeNamingConvention();
					var dlQueueName = _convetions.RetryQueueNamingConvention();
					var channel = _channelFactory.CreateChannel();
					channel.ExchangeDeclare(dlxName, ExchangeType.Direct);
					channel.QueueDeclare(dlQueueName, false, false, true, new Dictionary<string, object>
					{
						{"x-dead-letter-exchange", args.Exchange},
						{"x-message-ttl", Convert.ToInt32(timespan.TotalMilliseconds)}
					});
					channel.QueueBind(dlQueueName, dlxName, args.RoutingKey, null);
					channel.BasicPublish(dlxName, args.RoutingKey, args.BasicProperties, args.Body);
					Timer disposeChannel = null;
					disposeChannel = new Timer(state =>
					{
						channel.QueueDelete(dlQueueName); //TODO: investigate why auto-delete doesn't work?
						channel.Dispose();
						disposeChannel?.Dispose();
					}, null, timespan, new TimeSpan(-1));
				};
			}
		}
	}
}
