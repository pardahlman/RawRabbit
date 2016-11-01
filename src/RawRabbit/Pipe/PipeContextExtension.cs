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
		public static object GetMessage(this IPipeContext context)
		{
			return context.Get<object>(PipeKey.Message);
		}

		public static Type GetMessageType(this IPipeContext context)
		{
			return context.Get<Type>(PipeKey.MessageType);
		}

		public static IMessageContext GetMessageContext(this IPipeContext context)
		{
			return context.Get<IMessageContext>(PipeKey.MessageContext);
		}

		public static IRawConsumer GetConsumer(this IPipeContext context)
		{
			return context.Get<IRawConsumer>(PipeKey.Consumer);
		}

		public static QueueConfiguration GetQueueConfiguration(this IPipeContext context)
		{
			return context.Get<QueueConfiguration>(PipeKey.QueueConfiguration);
		}

		public static ExchangeConfiguration GetExchangeConfiguration(this IPipeContext context)
		{
			return context.Get<ExchangeConfiguration>(PipeKey.ExchangeConfiguration);
		}

		public static string GetExchangeName(this IPipeContext context)
		{
			return context.Get<string>(PipeKey.ExchangeName) ?? GetExchangeConfiguration(context)?.ExchangeName;
		}

		public static bool GetMandatoryPublishFlag(this IPipeContext context)
		{
			return GetReturnedMessageCallback(context) != null;
		}

		public static EventHandler<BasicReturnEventArgs> GetReturnedMessageCallback(this IPipeContext context)
		{
			return context.Get<EventHandler<BasicReturnEventArgs>>(PipeKey.ReturnedMessageCallback);
		}

		public static IConsumerConfiguration GetConsumerConfiguration(this IPipeContext context)
		{
			var routingKey = GetRoutingKey(context);
			var queueCfg = GetQueueConfiguration(context);
			var exchangeCfg = GetExchangeConfiguration(context);
			var noAck = context.Get<bool>(PipeKey.NoAck);
			var prefetch = context.Get<ushort>(PipeKey.PrefetchCount);
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
			return context.Get<string>(PipeKey.RoutingKey);
		}

		public static IModel GetChannel(this IPipeContext context)
		{
			return context.Get<IModel>(PipeKey.Channel);
		}

		public static Guid GetGlobalMessageId(this IPipeContext context)
		{
			return context.Get<Guid>(PipeKey.GlobalMessageId);
		}

		public static IBasicProperties GetBasicProperties(this IPipeContext context)
		{
			return GetDeliveryEventArgs(context)?.BasicProperties ?? context.Get<IBasicProperties>(PipeKey.BasicProperties);
		}

		public static BasicDeliverEventArgs GetDeliveryEventArgs(this IPipeContext context)
		{
			return context.Get<BasicDeliverEventArgs>(PipeKey.DeliveryEventArgs);
		}
	}
}