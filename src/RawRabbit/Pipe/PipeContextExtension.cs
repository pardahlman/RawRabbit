using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Configuration.Queue;
using ISubscription = RawRabbit.Subscription.ISubscription;

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

		public static object GetMessageContext(this IPipeContext context)
		{
			return context.Get<object>(PipeKey.MessageContext);
		}

		public static IBasicConsumer GetConsumer(this IPipeContext context)
		{
			return context.Get<IBasicConsumer>(PipeKey.Consumer);
		}

		public static QueueDeclaration GetQueueDeclaration(this IPipeContext context)
		{
			return context.Get<QueueDeclaration>(PipeKey.QueueDeclaration);
		}

		public static Action<Func<Task>, CancellationToken> GetConsumeThrottleAction(this IPipeContext context)
		{
			return context.Get<Action<Func<Task>, CancellationToken>>(PipeKey.ConsumeThrottleAction, (func, token) => func());
		}

		public static ExchangeDeclaration GetExchangeDeclaration(this IPipeContext context)
		{
			return context.Get<ExchangeDeclaration>(PipeKey.ExchangeDeclaration);
		}

		public static EventHandler<BasicReturnEventArgs> GetReturnedMessageCallback(this IPipeContext context)
		{
			return context.Get<EventHandler<BasicReturnEventArgs>>(PipeKey.ReturnedMessageCallback);
		}

		public static ConsumeConfiguration GetConsumeConfiguration(this IPipeContext context)
		{
			return context.Get<ConsumeConfiguration>(PipeKey.ConsumeConfiguration);
		}

		public static BasicPublishConfiguration GetBasicPublishConfiguration(this IPipeContext context)
		{
			return context.Get<BasicPublishConfiguration>(PipeKey.BasicPublishConfiguration);
		}

		public static ConsumerConfiguration GetConsumerConfiguration(this IPipeContext context)
		{
			return context.Get<ConsumerConfiguration>(PipeKey.ConsumerConfiguration);
		}

		public static PublisherConfiguration GetPublishConfiguration(this IPipeContext context)
		{
			return context.Get<PublisherConfiguration>(PipeKey.PublisherConfiguration);
		}

		public static string GetRoutingKey(this IPipeContext context)
		{
			return context.Get<string>(PipeKey.RoutingKey);
		}

		public static ISubscription GetSubscription(this IPipeContext context)
		{
			return context.Get<ISubscription>(PipeKey.Subscription);
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
			return context.Get<IBasicProperties>(PipeKey.BasicProperties);
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

		public static RawRabbitConfiguration GetClientConfiguration(this IPipeContext context)
		{
			return context.Get<RawRabbitConfiguration>(PipeKey.ClientConfiguration);
		}
	}
}
