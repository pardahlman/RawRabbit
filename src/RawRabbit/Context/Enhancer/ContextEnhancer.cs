using System;
using System.Collections.Generic;
using System.Linq;
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
			if (advancedCtx == null)
			{
				return;
			}
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
						{PropertyHeaders.DeadLetterExchange, args.Exchange},
						{PropertyHeaders.MessageTtl, Convert.ToInt32(timespan.TotalMilliseconds)}
				});
				channel.QueueBind(dlQueueName, dlxName, args.RoutingKey, null);
				AddEstimatedRetryHeader(args.BasicProperties);
				channel.BasicPublish(dlxName, args.RoutingKey, args.BasicProperties, args.Body);
				Timer disposeChannel = null;
				disposeChannel = new Timer(state =>
				{
					channel.QueueDelete(dlQueueName); //TODO: investigate why auto-delete doesn't work?
						channel.Dispose();
					disposeChannel?.Dispose();
				}, null, timespan.Add(TimeSpan.FromMilliseconds(20)), new TimeSpan(-1));
			};

			advancedCtx.RetryInfo = GetRetryInformatino(args.BasicProperties);
		}

		private static void AddEstimatedRetryHeader(IBasicProperties basicProperties)
		{
			if (basicProperties.Headers.ContainsKey(PropertyHeaders.EstimatedRetry))
			{
				basicProperties.Headers.Remove(PropertyHeaders.EstimatedRetry);
			}
			basicProperties.Headers.Add(PropertyHeaders.EstimatedRetry, DateTime.UtcNow.ToString("u"));
		}

		private static RetryInformation GetRetryInformatino(IBasicProperties basicProperties)
		{
			return new RetryInformation
			{
				OriginalSent = GetOriginalSentDate(basicProperties),
				NumberOfRetries = GetCurentRetryCount(basicProperties)
			};
		}

		private static DateTime GetOriginalSentDate(IBasicProperties basicProperties)
		{
			if (!basicProperties.Headers.ContainsKey(PropertyHeaders.Sent))
			{
				return DateTime.MinValue;
			}
			var sentBytes = basicProperties?.Headers[PropertyHeaders.Sent] as byte[] ?? new byte[0];
			var sentStr = System.Text.Encoding.UTF8.GetString(sentBytes);
			return DateTime.Parse(sentStr);
		}

		private static long GetCurentRetryCount(IBasicProperties basicProperties)
		{
			if (basicProperties.Headers.ContainsKey(PropertyHeaders.Death))
			{
				object retryCount = null;
				var deathDictionary = (basicProperties.Headers[PropertyHeaders.Death] as List<object>)?.FirstOrDefault() as IDictionary<string, object>;
				if (deathDictionary?.TryGetValue("count", out retryCount) ?? false)
				{
					return (long)retryCount;
				}
			}
			return 0;
		}
	}
}
