using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageSerializationOptions
	{
		public Func<IPipeContext, object> MessageFunc { get; set; }
		public Action<IPipeContext, string> PersistAction { get; set; }
	}

	public class BodySerializationMiddleware : Middleware
	{
		protected readonly Func<IPipeContext, object> MsgFunc;
		protected Action<IPipeContext, string> PersistAction;
		private readonly ISerializer _serializer;

		public BodySerializationMiddleware(ISerializer serializer, MessageSerializationOptions options = null)
		{
			_serializer = serializer;
			MsgFunc = options?.MessageFunc ?? (context => context.GetMessage());
			PersistAction = options?.PersistAction ?? ((c, s) => c.Properties.TryAdd(PipeKey.SerializedMessage, s));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var message = GetMessage(context);
			var serialized = SerializeMessage(message);
			AddSerializedMessageToContext(context, serialized);
			return Next.InvokeAsync(context, token);
		}

		protected virtual object GetMessage(IPipeContext context)
		{
			return MsgFunc(context);
		}

		protected virtual string SerializeMessage(object message)
		{
			return _serializer.Serialize(message);
		}

		protected virtual void AddSerializedMessageToContext(IPipeContext context, string serialized)
		{
			PersistAction?.Invoke(context, serialized);
		}
	}
}
