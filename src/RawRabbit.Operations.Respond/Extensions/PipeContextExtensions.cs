using System;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Extensions
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

		public static Func<object, Task<object>> GetMessageHandler(this IPipeContext context)
		{
			return context.Get<Func<object, Task<object>>>(PipeKey.MessageHandler);
		}
	}
}
