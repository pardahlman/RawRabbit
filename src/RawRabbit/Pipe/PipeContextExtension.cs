using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Get;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Configuration.Queue;
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

		public static QueueDeclaration GetQueueDeclaration(this IPipeContext context)
		{
			return context.Get<QueueDeclaration>(PipeKey.QueueDeclaration);
		}

		public static GetConfiguration GetGetConfiguration(this IPipeContext context)
		{
			return context.Get<GetConfiguration>(PipeKey.GetConfiguration);
		}

		public static ExchangeDeclaration GetExchangeDeclaration(this IPipeContext context)
		{
			return context.Get<ExchangeDeclaration>(PipeKey.ExchangeDeclaration);
		}

		public static bool GetMandatoryPublishFlag(this IPipeContext context)
		{
			return GetReturnedMessageCallback(context) != null;
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

		public static string GetGlobalExecutionId(this IPipeContext context)
		{
			return context.Get<string>(PipeKey.GlobalExecutionId);
		}

		public static IModel GetChannel(this IPipeContext context)
		{
			return context.Get<IModel>(PipeKey.Channel);
		}

		public static List<Action<IQueueDeclarationBuilder>> GetQueueActions(this IPipeContext context, Type type = null)
		{
			var typedActions = context.Get<ConcurrentDictionary<string, List<Action<IQueueDeclarationBuilder>>>>(PipeKey.QueueActions);
			if (typedActions == null)
			{
				typedActions = new ConcurrentDictionary<string, List<Action<IQueueDeclarationBuilder>>>();
				context.Properties.Add(PipeKey.QueueActions, typedActions);
			}
			var actions = typedActions.GetOrAdd(type?.FullName ?? string.Empty, t => new List<Action<IQueueDeclarationBuilder>>());
			return actions;
		}

		public static List<Action<IExchangeDeclarationBuilder>> GetExchangeActions(this IPipeContext context, Type type = null)
		{
			var typedActions = context.Get<ConcurrentDictionary<string, List<Action<IExchangeDeclarationBuilder>>>>(PipeKey.ExchangeActions);
			if (typedActions == null)
			{
				typedActions = new ConcurrentDictionary<string, List<Action<IExchangeDeclarationBuilder>>>();
				context.Properties.Add(PipeKey.ExchangeActions, typedActions);
			}
			var actions = typedActions.GetOrAdd(type?.FullName ?? string.Empty, t => new List<Action<IExchangeDeclarationBuilder>>());
			return actions;
		}

		public static List<Action<IConsumeConfigurationBuilder>> GetConsumeActions(this IPipeContext context, Type type = null)
		{
			var typedActions = context.Get<ConcurrentDictionary<string, List<Action<IConsumeConfigurationBuilder>>>>(PipeKey.ConsumeActions);
			if (typedActions == null)
			{
				typedActions = new ConcurrentDictionary<string, List<Action<IConsumeConfigurationBuilder>>>();
				context.Properties.Add(PipeKey.ConsumeActions, typedActions);
			}
			var actions = typedActions.GetOrAdd(type?.FullName ?? string.Empty, t => new List<Action<IConsumeConfigurationBuilder>>());
			return actions;
		}

		public static List<Action<IBasicPublishConfigurationBuilder>> GetBasicPublishActions(this IPipeContext context, Type type = null)
		{
			var typedActions = context.Get<ConcurrentDictionary<string, List<Action<IBasicPublishConfigurationBuilder>>>>(PipeKey.BasicPublishActions);
			if (typedActions == null)
			{
				typedActions = new ConcurrentDictionary<string, List<Action<IBasicPublishConfigurationBuilder>>>();
				context.Properties.Add(PipeKey.BasicPublishActions, typedActions);
			}
			var actions = typedActions.GetOrAdd(type?.FullName ?? string.Empty, t => new List<Action<IBasicPublishConfigurationBuilder>>());
			return actions;
		}

		public static IModel GetTransientChannel(this IPipeContext context)
		{
			return context.Get<IModel>(PipeKey.TransientChannel);
		}

		public static IBasicProperties GetBasicProperties(this IPipeContext context)
		{
			return context.Get<IBasicProperties>(PipeKey.BasicProperties) ?? GetDeliveryEventArgs(context)?.BasicProperties;
		}

		public static BasicDeliverEventArgs GetDeliveryEventArgs(this IPipeContext context)
		{
			return context.Get<BasicDeliverEventArgs>(PipeKey.DeliveryEventArgs);
		}

		public static BasicGetResult GetBasicGetResult(this IPipeContext context)
		{
			return context.Get<BasicGetResult>(PipeKey.BasicGetResult);
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