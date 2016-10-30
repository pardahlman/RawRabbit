using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.MessageContext.Subscribe.Middleware
{
	public class MessageContextDeserializationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly JsonSerializer _serializer;

		public MessageContextDeserializationMiddleware(JsonSerializer serializer)
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
			object messageContext;
			using (var jsonReader = new JsonTextReader(new StringReader(contextString)))
			{
				messageContext = _serializer.Deserialize<IMessageContext>(jsonReader);
			}
			if (messageContext == null)
			{
				throw new Exception();
			}
			context.Properties.Add(PipeKey.MessageContext, messageContext);
			return Next.InvokeAsync(context);
		}
	}
}
