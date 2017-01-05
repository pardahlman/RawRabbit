using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.Attributes.Middleware
{
	public class AttributeOptions
	{
		public Func<IPipeContext, ConsumerConfiguration> ConfigFunc { get; set; }
		public Func<IPipeContext, List<Type>> MessageTypeFunc { get; set; }
		public Func<IPipeContext, Type, List<Action<IQueueDeclarationBuilder>>> QueueActionsFunc { get; set; }
		public Func<IPipeContext, Type, List<Action<IExchangeDeclarationBuilder>>> ExchangeActionsFunc { get; set; }
		public Func<IPipeContext, Type, List<Action<IConsumeConfigurationBuilder>>> ConsumeActionsFunc { get; set; }
		public Func<IPipeContext, Type, List<Action<IBasicPublishConfigurationBuilder>>> PublishActionsFunc { get; set; }
	}

	public class AttributeMiddleware : StagedMiddleware
	{
		protected Func<IPipeContext, List<Type>> MessageTypeFunc;
		protected Func<IPipeContext, Type, List<Action<IExchangeDeclarationBuilder>>> ExchangeActionsFunc;
		protected Func<IPipeContext, Type, List<Action<IQueueDeclarationBuilder>>> QueueActionsFunc;
		protected Func<IPipeContext, Type, List<Action<IConsumeConfigurationBuilder>>> ConsumeActionsFunc;
		protected Func<IPipeContext, Type, List<Action<IBasicPublishConfigurationBuilder>>> PublishActionsFunc;

		public AttributeMiddleware(AttributeOptions options = null)
		{
			MessageTypeFunc = options?.MessageTypeFunc ?? (context => new List<Type> { context.GetMessageType() });
			ExchangeActionsFunc = options?.ExchangeActionsFunc ?? ((context, type) => context.GetExchangeActions());
			QueueActionsFunc = options?.QueueActionsFunc ?? ((context, type) => context.GetQueueActions(type));
			ConsumeActionsFunc = options?.ConsumeActionsFunc ?? ((context, type) => context.GetConsumeActions(type));
			PublishActionsFunc = options?.PublishActionsFunc ?? ((context, type) => context.GetBasicPublishActions(type));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var msgTypes = GetMessageTypes(context);
			foreach (var msgType in msgTypes)
			{
				if (msgType == null)
				{
					continue;
				}

				var exchangeActions = GetExchangeActions(context, msgType);
				var exchangeAction = GetExchangeAction(msgType);
				exchangeActions.Add(exchangeAction);

				var queueActions = GetQueueActions(context, msgType);
				var queueAction = GetQueueAction(msgType);
				queueActions.Add(queueAction);

				var consumeActions = GetConsumeActions(context, msgType);
				var consumeAction = GetConsumeAction(msgType);
				consumeActions.Add(consumeAction);

				var publishActions = GetPublishActions(context, msgType);
				var publishAction = GetPublishAction(msgType);
				publishActions.Add(publishAction);
			}

			return Next.InvokeAsync(context, token);
		}

		protected virtual List<Type> GetMessageTypes(IPipeContext context)
		{
			return MessageTypeFunc(context);
		}

		protected virtual Action<IBasicPublishConfigurationBuilder> GetPublishAction(Type type)
		{
			Action<IBasicPublishConfigurationBuilder> action = builder => { };

			var routingAttr = GetAttribute<RoutingAttribute>(type);
			if (routingAttr?.RoutingKey != null)
			{
				action += builder => builder.WithRoutingKey(routingAttr.RoutingKey);
			}
			return action;
		}

		protected virtual Action<IConsumeConfigurationBuilder> GetConsumeAction(Type type)
		{
			Action<IConsumeConfigurationBuilder> action = builder => { };
			var routingAttr = GetAttribute<RoutingAttribute>(type);
			if (routingAttr?.RoutingKey != null)
			{
				action += builder => builder.WithRoutingKey(routingAttr.RoutingKey);
			}
			if (routingAttr?.NullableNoAck != null)
			{
				action += builder => builder.WithNoAck(routingAttr.NoAck);
			}
			return action;
		}

		protected virtual Action<IExchangeDeclarationBuilder> GetExchangeAction(Type type)
		{
			Action<IExchangeDeclarationBuilder> exchangeAction = b => { };
			var exchangeAttr = GetAttribute<ExchangeAttribute>(type);

			if (exchangeAttr == null)
			{
				return exchangeAction;
			}
			if (!string.IsNullOrWhiteSpace(exchangeAttr.Name))
			{
				exchangeAction += builder => builder.WithName(exchangeAttr.Name);
			}
			if (exchangeAttr.NullableDurability.HasValue)
			{
				exchangeAction += builder => builder.WithDurability(exchangeAttr.NullableDurability.Value);
			}
			if (exchangeAttr.NullableAutoDelete.HasValue)
			{
				exchangeAction += builder => builder.WithDurability(exchangeAttr.NullableAutoDelete.Value);
			}
			if (exchangeAttr.Type != ExchangeType.Unknown)
			{
				exchangeAction += builder => builder.WithType(exchangeAttr.Type);
			}
			return exchangeAction;
		}

		protected virtual Action<IQueueDeclarationBuilder> GetQueueAction(Type type)
		{
			Action<IQueueDeclarationBuilder> queueAction = builder => { };

			var queueAttr = GetAttribute<QueueAttribute>(type);
			if (queueAttr == null)
			{
				return queueAction;
			}
			if (!string.IsNullOrWhiteSpace(queueAttr.Name))
			{
				queueAction += builder => builder.WithName(queueAttr.Name);
			}
			if (queueAttr.NullableDurability.HasValue)
			{
				queueAction += builder => builder.WithDurability(queueAttr.NullableDurability.Value);
			}
			if (queueAttr.NullableExclusitivy.HasValue)
			{
				queueAction += builder => builder.WithExclusivity(queueAttr.NullableExclusitivy.Value);
			}
			if (queueAttr.NullableAutoDelete.HasValue)
			{
				queueAction += builder => builder.WithAutoDelete(queueAttr.NullableAutoDelete.Value);
			}
			if (queueAttr.MessageTtl > 0)
			{
				queueAction += builder => builder.WithArgument(QueueArgument.MessageTtl, queueAttr.MessageTtl);
			}
			if (queueAttr.MaxPriority > 0)
			{
				queueAction += builder => builder.WithArgument(QueueArgument.MaxPriority, queueAttr.MaxPriority);
			}
			if (!string.IsNullOrWhiteSpace(queueAttr.DeadLeterExchange))
			{
				queueAction += builder => builder.WithArgument(QueueArgument.DeadLetterExchange, queueAttr.DeadLeterExchange);
			}
			if (!string.IsNullOrWhiteSpace(queueAttr.Mode))
			{
				queueAction += builder => builder.WithArgument(QueueArgument.QueueMode, queueAttr.Mode);
			}
			return queueAction;
		}

		protected virtual TAttribute GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
		{
			var attr = type.GetTypeInfo().GetCustomAttribute<TAttribute>();
			return attr;
		}

		protected virtual List<Action<IExchangeDeclarationBuilder>> GetExchangeActions(IPipeContext context, Type msgType)
		{
			return ExchangeActionsFunc(context, msgType);
		}

		protected virtual List<Action<IQueueDeclarationBuilder>> GetQueueActions(IPipeContext context, Type msgType)
		{
			return QueueActionsFunc(context, msgType);
		}

		protected virtual List<Action<IConsumeConfigurationBuilder>> GetConsumeActions(IPipeContext context, Type msgType)
		{
			return ConsumeActionsFunc(context, msgType);
		}

		protected virtual List<Action<IBasicPublishConfigurationBuilder>> GetPublishActions(IPipeContext context, Type msgType)
		{
			return PublishActionsFunc(context, msgType);
		}

		public override string StageMarker => Pipe.StageMarker.Initialized;
	}
}
