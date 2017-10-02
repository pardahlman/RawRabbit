using System;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using RawRabbit.Serialization;

namespace RawRabbit.Operations.Request.Middleware
{
	public class BasicPropertiesMiddleware : Pipe.Middleware.BasicPropertiesMiddleware
	{
		public BasicPropertiesMiddleware(ISerializer serializer, BasicPropertiesOptions options) :base(serializer, options)
		{ }

		protected override void ModifyBasicProperties(IPipeContext context, IBasicProperties props)
		{
			var correlationId = context.GetCorrelationId() ?? Guid.NewGuid().ToString();
			var consumeCfg = context.GetResponseConfiguration();
			var clientCfg = context.GetClientConfiguration();

			if (consumeCfg.Consume.IsDirectReplyTo() || consumeCfg.Exchange == null)
			{
				props.ReplyTo = consumeCfg.Consume.QueueName;
			}
			else
			{
				props.ReplyToAddress = new PublicationAddress(consumeCfg.Exchange.ExchangeType, consumeCfg.Exchange.Name, consumeCfg.Consume.RoutingKey);
			}

			props.CorrelationId = correlationId;
			props.Expiration = clientCfg.RequestTimeout.TotalMilliseconds.ToString();
		}
	}
}
