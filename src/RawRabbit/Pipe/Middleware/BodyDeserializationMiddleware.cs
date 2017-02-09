using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageDeserializationOptions
	{
		public Func<IPipeContext, Type> BodyTypeFunc { get; set; }
		public Func<IPipeContext, byte[]> BodyFunc { get; set; }
		public Action<IPipeContext, object> PersistAction { get; set; }
	}

	public class BodyDeserializationMiddleware : Middleware
	{
		protected readonly ISerializer Serializer;
		protected Func<IPipeContext, Type> MessageTypeFunc;
		protected Func<IPipeContext, byte[]> BodyBytesFunc;
		protected Action<IPipeContext, object> PersistAction;
		private readonly ILogger _logger = LogManager.GetLogger<BodyDeserializationMiddleware>();

		public BodyDeserializationMiddleware(ISerializer serializer, MessageDeserializationOptions options = null)
		{
			Serializer = serializer;
			MessageTypeFunc = options?.BodyTypeFunc ?? (context => context.GetMessageType());
			BodyBytesFunc = options?.BodyFunc ?? (context =>context.GetDeliveryEventArgs()?.Body);
			PersistAction = options?.PersistAction ?? ((context, msg) => context.Properties.TryAdd(PipeKey.Message, msg));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var message = GetMessage(context);
			SaveInContext(context, message);
			return Next.InvokeAsync(context, token);
		}

		protected virtual object GetMessage(IPipeContext context)
		{
			var serialized = GetSerializedMessage(context);
			var messageType = GetMessageType(context);
			return  Serializer.Deserialize(messageType, serialized);
		}

		protected virtual string GetSerializedMessage(IPipeContext context)
		{
			var bodyBytes = GetBodyBytes(context);
			var serialized = Encoding.UTF8.GetString(bodyBytes ?? new byte[0]);
			return serialized;
		}

		protected virtual Type GetMessageType(IPipeContext context)
		{
			var msgType = MessageTypeFunc(context);
			if (msgType == null)
			{
				_logger.LogWarning("Unable to find message type in Pipe context.");
			}
			return msgType;
		}

		protected virtual byte[] GetBodyBytes(IPipeContext context)
		{
			var bodyBytes = BodyBytesFunc(context);
			if (bodyBytes == null)
			{
				_logger.LogWarning("Unable to find Body (bytes) in Pipe context");
			}
			return bodyBytes;
		}

		protected virtual void SaveInContext(IPipeContext context, object message)
		{
			if (PersistAction == null)
			{
				_logger.LogWarning("No persist action defined. Message will not be saved in Pipe context.");
			}
			PersistAction?.Invoke(context, message);
		}
	}
}
