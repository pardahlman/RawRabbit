using System;
using System.Collections.Generic;
using RabbitMQ.Client.Events;

namespace RawRabbit.Common
{
	public interface IRetryInformationHeaderUpdater
	{
		void AddOrUpdate(BasicDeliverEventArgs args);
		void AddOrUpdate(BasicDeliverEventArgs args, RetryInformation retryInfo);
	}

	public class RetryInformationHeaderUpdater : IRetryInformationHeaderUpdater
	{
		public void AddOrUpdate(BasicDeliverEventArgs args)
		{
			TryAddOriginalDelivered(args, DateTime.UtcNow);
			AddOrUpdateNumberOfRetries(args);
		}

		public void AddOrUpdate(BasicDeliverEventArgs args, RetryInformation retryInfo)
		{
			TryAddOriginalDelivered(args, retryInfo.OriginalDelivered);
			AddOrUpdateNumberOfRetries(args);
		}

		private void AddOrUpdateNumberOfRetries(BasicDeliverEventArgs args)
		{
			var currentRetry = 0;
			if (args.BasicProperties.Headers.ContainsKey(RetryHeaders.NumberOfRetries))
			{
				var valueStr = GetHeaderString(args.BasicProperties.Headers, RetryHeaders.NumberOfRetries);
				currentRetry = int.Parse(valueStr);
				args.BasicProperties.Headers.Remove(RetryHeaders.NumberOfRetries);
			}
			var nextRetry = (++currentRetry).ToString();
			args.BasicProperties.Headers.Add(RetryHeaders.NumberOfRetries, nextRetry);
		}

		private static void TryAddOriginalDelivered(BasicDeliverEventArgs args, DateTime originalDelivered)
		{
			if (args.BasicProperties.Headers.ContainsKey(RetryHeaders.OriginalDelivered))
			{
				return;
			}
			args.BasicProperties.Headers.Add(RetryHeaders.OriginalDelivered, originalDelivered.ToString("u"));
		}

		private static string GetHeaderString(IDictionary<string, object> headers, string key)
		{
			if (headers == null)
			{
				return null;
			}
			if (!headers.ContainsKey(key))
			{
				return null;
			}
			if (!(headers[key] is byte[] headerBytes))
			{
				return null;
			}

			var headerStr = System.Text.Encoding.UTF8.GetString(headerBytes);
				return headerStr;
		}
	}
}
