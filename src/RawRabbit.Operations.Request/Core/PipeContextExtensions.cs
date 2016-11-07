using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing.Impl;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Core
{
	public static class PipeContextExtensions
	{
		public static Type GetResponseMessageType(this IPipeContext context)
		{
			return context.Get<Type>(RequestKey.ResponseMessageType);
		}

		public static string GetCorrelationId(this IPipeContext context)
		{
			return context.Get<string>(RequestKey.CorrelationId);
		}

		public static Type GetRequestMessageType(this IPipeContext context)
		{
			return context.Get<Type>(RequestKey.RequestMessageType);
		}

		public static Func<object, Task<object>> GetMessageHandler(this IPipeContext context)
		{
			return context.Get<Func<object, Task<object>>>(PipeKey.MessageHandler);
		}

		public static PublicationAddress GetPublicationAddress(this IPipeContext context)
		{
			return context.Get<PublicationAddress>(RequestKey.PublicationAddress);
		}

		public static QueueConfiguration GetResponseQueue(this IPipeContext context)
		{
			return context.GetRequestConfiguration()?.Response.Queue;
		}

		public static ExchangeConfiguration GetRequestExchange(this IPipeContext context)
		{
			return context.GetRequestConfiguration()?.Request.Exchange;
		}

		public static ExchangeConfiguration GetResponseExchange(this IPipeContext context)
		{
			return context.GetRequestConfiguration()?.Response.Exchange;
		}

		public static RequestConfiguration GetRequestConfiguration(this IPipeContext context)
		{
			return context.Get<RequestConfiguration>(RequestKey.Configuration);
		}

		public static ConsumeConfiguration GetResponseConfiguration(this IPipeContext context)
		{
			return context.GetRequestConfiguration()?.Response;
		}
	}
}
