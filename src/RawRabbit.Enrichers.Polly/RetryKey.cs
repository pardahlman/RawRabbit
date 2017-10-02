using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Consumer;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly
{
	public class RetryKey
	{
		public const string PipeContext = "PipeContext";
		public const string PublishMandatory = "PublishMandatory";
		public const string BasicProperties = "BasicProperties";
		public const string PublishBody = "PublishBody";
		public const string QueueDeclaration = "QueueDeclaration";
		public const string QueueName = "QueueName";
		public const string ExchangeDeclaration = "ExchangeDeclaration";
		public const string ExchangeName = "ExchangeName";
		public const string RoutingKey = "RoutingKey";
		public const string CancellationToken = "CancellationToken";
		public const string TopologyProvider = "TopologyProvider";
		public const string ChannelFactory = "ChannelFactory";
		public const string ConsumerFactory = "ConsumerFactory";
	}

	public static class RetryExtensions
	{
		public static IPipeContext GetPipeContext(this IDictionary<string, object> ctx)
		{
			return ctx.Get<IPipeContext>(RetryKey.PipeContext);
		}

		public static ITopologyProvider GetTopologyProvider(this IDictionary<string, object> ctx)
		{
			return ctx.Get<ITopologyProvider>(RetryKey.TopologyProvider);
		}

		public static IConsumerFactory GetConsumerFactory(this IDictionary<string, object> ctx)
		{
			return ctx.Get<IConsumerFactory>(RetryKey.ConsumerFactory);
		}

		public static IChannelFactory GetChannelFactory(this IDictionary<string, object> ctx)
		{
			return ctx.Get<IChannelFactory>(RetryKey.ChannelFactory);
		}

		public static ExchangeDeclaration GetExchangeDeclaration(this IDictionary<string, object> ctx)
		{
			return ctx.Get<ExchangeDeclaration>(RetryKey.ExchangeDeclaration);
		}

		public static QueueDeclaration GetQueueDeclaration(this IDictionary<string, object> ctx)
		{
			return ctx.Get<QueueDeclaration>(RetryKey.QueueDeclaration);
		}

		public static string GetQueueName(this IDictionary<string, object> ctx)
		{
			return ctx.Get<string>(RetryKey.QueueName);
		}

		public static string GetExchangeName(this IDictionary<string, object> ctx)
		{
			return ctx.Get<string>(RetryKey.ExchangeName);
		}

		public static string GetRoutingKey(this IDictionary<string, object> ctx)
		{
			return ctx.Get<string>(RetryKey.RoutingKey);
		}

		private static TType Get<TType>(this IDictionary<string, object> ctx, string key, TType fallback = default(TType))
		{
			if (!ctx?.ContainsKey(key) ?? true)
			{
				return fallback;
			}
			var value = ctx[key];
			if (value is TType)
			{
				return (TType) value;
			}
			return fallback;
		}
	}
}
