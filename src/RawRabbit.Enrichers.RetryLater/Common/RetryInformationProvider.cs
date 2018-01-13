using System;
using System.Collections.Generic;
using RabbitMQ.Client.Events;

namespace RawRabbit.Common
{
	public interface IRetryInformationProvider
	{
		RetryInformation Get(BasicDeliverEventArgs args);
	}

	public class RetryInformationProvider : IRetryInformationProvider
	{
		public RetryInformation Get(BasicDeliverEventArgs args)
		{
			return new RetryInformation
			{
				NumberOfRetries = ExtractNumberOfRetries(args),
				OriginalDelivered = ExtractOriginalDelivered(args)
			};
		}

		private DateTime ExtractOriginalDelivered(BasicDeliverEventArgs args)
		{
			var headerValue = GetHeaderString(args.BasicProperties.Headers, RetryHeaders.OriginalDelivered);
			return DateTime.TryParse(headerValue, out var originalSent) ? originalSent : DateTime.UtcNow;
		}

		private int ExtractNumberOfRetries(BasicDeliverEventArgs args)
		{
			var headerValue = GetHeaderString(args.BasicProperties.Headers, RetryHeaders.NumberOfRetries);
			return int.TryParse(headerValue, out var noOfRetries) ? noOfRetries : 0;
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
