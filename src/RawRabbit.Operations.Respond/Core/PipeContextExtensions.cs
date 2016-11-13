using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Core
{
	public static class PipeContextExtensions
	{
		public static Type GetResponseMessageType(this IPipeContext context)
		{
			return context.Get<Type>(RespondKey.ResponseMessageType);
		}

		public static Type GetRequestMessageType(this IPipeContext context)
		{
			return context.Get<Type>(RespondKey.RequestMessageType);
		}

		public static Func<object, Task<object>> GetResponseMessageHandler(this IPipeContext context)
		{
			return context.Get<Func<object, Task<object>>>(PipeKey.MessageHandler);
		}

		public static PublicationAddress GetPublicationAddress(this IPipeContext context)
		{
			return context.Get<PublicationAddress>(RespondKey.PublicationAddress);
		}

		public static RespondConfiguration GetRespondConfiguration(this IPipeContext context)
		{
			return context.Get<RespondConfiguration>(RespondKey.Configuration);
		}
	}
}
