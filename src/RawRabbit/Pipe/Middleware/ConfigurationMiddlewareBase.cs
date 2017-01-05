using System;
using System.Collections.Generic;
using System.Linq;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Pipe.Middleware
{
	public abstract class ConfigurationMiddlewareBase : Middleware
	{
		protected virtual List<Action<IQueueDeclarationBuilder>> GetQueueActions(IPipeContext context, Type messageType)
		{
			return Enumerable
				.Concat(
					context.GetQueueActions(),
					messageType != null
						? context.GetQueueActions(messageType)
						: Enumerable.Empty<Action<IQueueDeclarationBuilder>>()
				)
				.ToList();
		}

		protected virtual List<Action<IExchangeDeclarationBuilder>> GetExchangeActions(IPipeContext context, Type messageType)
		{
			return Enumerable
				.Concat(
					context.GetExchangeActions(),
					messageType != null
						? context.GetExchangeActions(messageType)
						: Enumerable.Empty<Action<IExchangeDeclarationBuilder>>()
				)
				.ToList();
		}

		protected virtual List<Action<IConsumeConfigurationBuilder>> GetConsumeActions(IPipeContext context, Type messageType = null)
		{
			return Enumerable
					.Concat(
						context.GetConsumeActions(),
						messageType != null
							? context.GetConsumeActions(messageType)
							: Enumerable.Empty<Action<IConsumeConfigurationBuilder>>()
					)
					.ToList();
		}

		protected virtual List<Action<IBasicPublishConfigurationBuilder>> GetPublishActions(IPipeContext context, Type messageType = null)
		{
			return Enumerable
					.Concat(
						context.GetBasicPublishActions(),
						messageType != null
							? context.GetBasicPublishActions(messageType)
							: Enumerable.Empty<Action<IBasicPublishConfigurationBuilder>>()
					)
					.ToList();
		}

		protected virtual void InvokeQueueActions(IPipeContext context, Type msgType, QueueDeclaration queueCfg)
		{
			foreach (var queueAction in GetQueueActions(context, msgType))
			{
				var queue = new QueueDeclarationBuilder(queueCfg);
				queueAction?.Invoke(queue);
			}
		}

		protected virtual void InvokeExchangeActions(IPipeContext context, Type msgType, ExchangeDeclaration exchangeCfg)
		{
			foreach (var exchangeAction in GetExchangeActions(context, msgType))
			{
				var exchange = new ExchangeDeclarationBuilder(exchangeCfg);
				exchangeAction?.Invoke(exchange);
			}
		}

		protected virtual void InvokeConsumeActions(IPipeContext context, Type msgType, ConsumeConfiguration consumeCfg)
		{
			foreach (var consumeActions in GetConsumeActions(context, msgType))
			{
				var consume = new ConsumeConfigurationBuilder(consumeCfg);
				consumeActions?.Invoke(consume);
			}
		}

		protected virtual void InvokePublishActions(IPipeContext context, Type msgType, BasicPublishConfiguration publishCfg)
		{
			foreach (var publishActions in GetPublishActions(context, msgType))
			{
				var consume = new BasicPublishConfigurationBuilder(publishCfg);
				publishActions?.Invoke(consume);
			}
		}
	}
}
