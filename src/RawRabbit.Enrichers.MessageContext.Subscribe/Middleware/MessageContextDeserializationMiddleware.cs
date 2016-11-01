using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Pipe;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessageContext.Subscribe.Middleware
{
	public class MessageContextDeserializationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISerializer _serializer;

		public MessageContextDeserializationMiddleware(ISerializer serializer)
		{
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var args = context.GetDeliveryEventArgs();
			if (args == null)
			{
				throw new ArgumentNullException(nameof(args));
			}
			object contextBytes;
			if (!args.BasicProperties.Headers.TryGetValue(PropertyHeaders.Context, out contextBytes))
			{
				return Next.InvokeAsync(context);
			}

			var contextString = Encoding.UTF8.GetString((byte[])contextBytes);
			object messageContext = _serializer.Deserialize<IMessageContext>(contextString); ;
			if (messageContext == null)
			{
				throw new Exception();
			}
			context.Properties.Add(PipeKey.MessageContext, messageContext);
			return Next.InvokeAsync(context);
		}
	}
}
