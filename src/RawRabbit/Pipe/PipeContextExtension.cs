using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Consume;
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

		public static IBasicConsumer GetConsumer(this IPipeContext context)
		{
			return context.Get<IBasicConsumer>(PipeKey.Consumer);
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

		public static ConsumeConfiguration GetConsumerConfiguration(this IPipeContext context)
		{
			return context.Get<ConsumeConfiguration>(PipeKey.ConsumerConfiguration);
		}

		public static PublishConfiguration GetPublishConfiguration(this IPipeContext context)
		{
			return context.Get<PublishConfiguration>(PipeKey.PublishConfiguration);
		}

		public static string GetRoutingKey(this IPipeContext context)
		{
			return context.Get<string>(PipeKey.RoutingKey);
		}

		public static IModel GetChannel(this IPipeContext context)
		{
			return context.Get<IModel>(PipeKey.Channel);
		}

		public static IModel GetTransientChannel(this IPipeContext context)
		{
			return context.Get<IModel>(PipeKey.TransientChannel);
		}

		public static IBasicProperties GetBasicProperties(this IPipeContext context)
		{
			return GetDeliveryEventArgs(context)?.BasicProperties ?? context.Get<IBasicProperties>(PipeKey.BasicProperties);
		}

		public static BasicDeliverEventArgs GetDeliveryEventArgs(this IPipeContext context)
		{
			return context.Get<BasicDeliverEventArgs>(PipeKey.DeliveryEventArgs);
		}

		public static Func<object[], Task> GetMessageHandler(this IPipeContext context)
		{
			return context.Get<Func<object[], Task>>(PipeKey.MessageHandler);
		}

		public static object[] GetMessageHandlerArgs(this IPipeContext context)
		{
			return context.Get<object[]>(PipeKey.MessageHandlerArgs);
		}

		public static Task GetMessageHandlerResult(this IPipeContext context)
		{
			return context.Get<Task>(PipeKey.MessageHandlerResult);
		}
	}
}