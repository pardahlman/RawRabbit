using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageDeserializationOptions
	{
		public Func<IPipeContext, Type> BodyTypeFunc { get; set; }
		public Func<IPipeContext, string> MessageKeyFunc { get; set; }
		public Func<IPipeContext, byte[]> BodyFunc { get; set; }
		public Predicate<IPipeContext> AbortPredicate { get; set; }
	}

	public class BodyDeserializationMiddleware : Middleware
	{
		private readonly ISerializer _serializer;
		private readonly Func<IPipeContext, Type> _messageTypeFunc;
		private readonly Func<IPipeContext, byte[]> _bodyFunc;
		private readonly Func<IPipeContext, string> _messageKeyFunc;
		private readonly Predicate<IPipeContext> _abortPredicate;

		public BodyDeserializationMiddleware(ISerializer serializer, MessageDeserializationOptions options = null)
		{
			_serializer = serializer;
			_messageTypeFunc = options?.BodyTypeFunc ?? (context =>
			{
				var type = context.GetDeliveryEventArgs()?.BasicProperties?.Type;
				return !string.IsNullOrWhiteSpace(type) ? Type.GetType(type, false) : context.GetMessageType();
			});
			_messageKeyFunc = options?.MessageKeyFunc ?? (context => PipeKey.Message);
			_bodyFunc = options?.BodyFunc ?? (context =>context.GetDeliveryEventArgs()?.Body ?? context.GetBasicGetResult()?.Body);
			_abortPredicate = options?.AbortPredicate ?? (context => false);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			if (_abortPredicate(context))
			{
				return Next.InvokeAsync(context, token);
			}
			var bodyBytes = _bodyFunc(context);
			var body = Encoding.UTF8.GetString(bodyBytes);
			var messageType = _messageTypeFunc(context);
			var messageKey = _messageKeyFunc(context);

			var message = _serializer.Deserialize(messageType, body);
			context.Properties.Add(messageKey, message);
			return Next.InvokeAsync(context, token);
		}
	}
}
