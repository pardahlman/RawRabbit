using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context;

namespace RawRabbit.Pipe
{
	public static class PipeContextExtension
	{
		public static Operation GetOperation(this IPipeContext context)
		{
			return Get<Operation>(context, PipeKey.Operation);
		}

		public static object GetMessage(this IPipeContext context)
		{
			return Get<object>(context, PipeKey.Message);
		}

		public static Func<object, IMessageContext, Task> GetMessageHandler(this IPipeContext context)
		{
			return Get<Func<object, IMessageContext, Task>>(context, PipeKey.MessageHandler);
		}

		public static Type GetMessageType(this IPipeContext context)
		{
			return Get<Type>(context, PipeKey.MessageType);
		}

		public static IMessageContext GetMessageContext(this IPipeContext context)
		{
			return Get<IMessageContext>(context, PipeKey.MessageContext);
		}

		public static IRawConsumer GetConsumer(this IPipeContext context)
		{
			return Get<IRawConsumer>(context, PipeKey.Consumer);
		}

		public static QueueConfiguration GetQueueConfiguration(this IPipeContext context)
		{
			return Get<QueueConfiguration>(context, PipeKey.QueueConfiguration);
		}

		public static ExchangeConfiguration GetExchangeConfiguration(this IPipeContext context)
		{
			return Get<ExchangeConfiguration>(context, PipeKey.ExchangeConfiguration);
		}

		public static IConsumerConfiguration GetConsumerConfiguration(this IPipeContext context)
		{
			var routingKey = GetRoutingKey(context);
			var queueCfg = GetQueueConfiguration(context);
			var exchangeCfg = GetExchangeConfiguration(context);
			var noAck = Get<bool>(context, PipeKey.NoAck);
			var prefetch = Get<ushort>(context, PipeKey.PrefetchCount);
			return new SubscriptionConfiguration
			{
				RoutingKey = routingKey,
				Queue = queueCfg,
				Exchange = exchangeCfg,
				NoAck = noAck,
				PrefetchCount = prefetch
			};
		}

		public static string GetRoutingKey(this IPipeContext context)
		{
			return Get<string>(context, PipeKey.RoutingKey);
		}

		public static IModel GetChannel(this IPipeContext context)
		{
			return Get<IModel>(context, PipeKey.Channel);
		}

		public static Guid GetGlobalMessageId(this IPipeContext context)
		{
			return Get<Guid>(context, PipeKey.GlobalMessageId);
		}

		public static IBasicProperties GetBasicProperties(this IPipeContext context)
		{
			return GetDeliveryEventArgs(context)?.BasicProperties ?? Get<IBasicProperties>(context, PipeKey.BasicProperties);
		}

		public static BasicDeliverEventArgs GetDeliveryEventArgs(this IPipeContext context)
		{
			return Get<BasicDeliverEventArgs>(context, PipeKey.DeliveryEventArgs);
		}

		public static TType Get<TType>(this IPipeContext context, string key, TType fallback = default(TType))
		{
			if (context?.Properties == null)
			{
				return fallback;
			}
			object result;
			if (context.Properties.TryGetValue(key, out result))
			{
				return result is TType ? (TType)result : fallback;
			}
			return fallback;
		}
	}
}