using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageSerializationOptions
	{
		public Func<IPipeContext, object> MessageFunc { get; set; }
		public Action<IPipeContext, byte[]> PersistAction { get; set; }
	}

	public class BodySerializationMiddleware : Middleware
	{
		protected readonly Func<IPipeContext, object> MsgFunc;
		protected Action<IPipeContext, byte[]> PersistAction;
		private readonly ISerializer _serializer;

		public BodySerializationMiddleware(ISerializer serializer, MessageSerializationOptions options = null)
		{
			_serializer = serializer;
			MsgFunc = options?.MessageFunc ?? (context => context.GetMessage());
			PersistAction = options?.PersistAction ?? ((c, s) => c.Properties.TryAdd(PipeKey.SerializedMessage, s));
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var message = GetMessage(context);
			var serialized = SerializeMessage(message);
			AddSerializedMessageToContext(context, serialized);
			await Next.InvokeAsync(context, token);
		}

		protected virtual object GetMessage(IPipeContext context)
		{
			return MsgFunc(context);
		}

		protected virtual byte[] SerializeMessage(object message)
		{
			return _serializer.Serialize(message);
		}

		protected virtual void AddSerializedMessageToContext(IPipeContext context, byte[] serialized)
		{
			PersistAction?.Invoke(context, serialized);
		}
	}
}
