using System;
using RabbitMQ.Client;
using RawRabbit.Configuration.Queue;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class BasicPropertiesMiddleware : Pipe.Middleware.BasicPropertiesMiddleware
	{
		protected override void ModifyBasicProperties(IPipeContext context, IBasicProperties props)
		{
			var correlationId = context.GetCorrelationId() ?? Guid.NewGuid().ToString();
			var consumeCfg = context.GetResponseConfiguration();
			var clientCfg = context.GetClientConfiguration();

			if (consumeCfg.Queue.IsDirectReplyTo())
			{
				props.ReplyTo = consumeCfg.Queue.QueueName;
			}
			else
			{
				props.ReplyToAddress = new PublicationAddress(consumeCfg.Exchange.ExchangeType, consumeCfg.Exchange.ExchangeName, consumeCfg.RoutingKey);
			}

			props.CorrelationId = correlationId;
			props.Expiration = clientCfg.RequestTimeout.TotalMilliseconds.ToString();
		}
	}
}
