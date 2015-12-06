using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Consumer.Abstraction;

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
				UpdateHeaders(args.BasicProperties);
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

		private static void UpdateHeaders(IBasicProperties basicProperties)
		{
			if (basicProperties.Headers.ContainsKey(PropertyHeaders.EstimatedRetry))
			{
				basicProperties.Headers.Remove(PropertyHeaders.EstimatedRetry);
			}
			basicProperties.Headers.Add(PropertyHeaders.EstimatedRetry, DateTime.UtcNow.ToString("u"));

			var currentRetry = 0;
			if (basicProperties.Headers.ContainsKey(PropertyHeaders.RetryCount))
			{
				var valueStr = GetHeaderString(basicProperties.Headers, PropertyHeaders.RetryCount);
				currentRetry = int.Parse(valueStr);
				basicProperties.Headers.Remove(PropertyHeaders.RetryCount);
			}
			var nextRetry = (++currentRetry).ToString();
			basicProperties.Headers.Add(PropertyHeaders.RetryCount, nextRetry);
		}

		private static string GetHeaderString(IDictionary<string, object> headers, string key)
		{
			var headerBytes = headers[key] as byte[] ?? new byte[0];
			var headerStr = System.Text.Encoding.UTF8.GetString(headerBytes);
			return headerStr;
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
			if (basicProperties.Headers.ContainsKey(PropertyHeaders.RetryCount))
			{
				var countStr = GetHeaderString(basicProperties.Headers, PropertyHeaders.RetryCount);
				return int.Parse(countStr);
			}
			return 0;
		}
	}
}
