using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.Impl;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Configuration.Abstraction;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Core
{
	public static class PipeContextExtensions
	{
		public static Type GetResponseMessageType(this IPipeContext context)
		{
			return context.Get<Type>(RequestKey.IncommingMessageType);
		}

		public static object GetResponseMessage(this IPipeContext context)
		{
			return context.Get<object>(RequestKey.ResponseMessage);
		}

		public static string GetCorrelationId(this IPipeContext context)
		{
			return context.Get<string>(RequestKey.CorrelationId);
		}

		public static Type GetRequestMessageType(this IPipeContext context)
		{
			return context.Get<Type>(RequestKey.OutgoingMessageType);
		}

		public static PublicationAddress GetPublicationAddress(this IPipeContext context)
		{
			return context.Get<PublicationAddress>(RequestKey.PublicationAddress);
		}

		public static QueueDeclaration GetResponseQueue(this IPipeContext context)
		{
			return context.GetRequestConfiguration()?.Response.Queue;
		}

		public static ExchangeDeclaration GetRequestExchange(this IPipeContext context)
		{
			return context.GetRequestConfiguration()?.Request.Exchange;
		}

		public static ExchangeDeclaration GetResponseExchange(this IPipeContext context)
		{
			return context.GetRequestConfiguration()?.Response.Exchange;
		}

		public static RequestConfiguration GetRequestConfiguration(this IPipeContext context)
		{
			return context.Get<RequestConfiguration>(RequestKey.Configuration);
		}

		public static ConsumerConfiguration GetResponseConfiguration(this IPipeContext context)
		{
			return context.GetRequestConfiguration()?.Response;
		}

	}
}
