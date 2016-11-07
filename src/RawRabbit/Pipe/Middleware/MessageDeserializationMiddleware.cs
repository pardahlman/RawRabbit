using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageDeserializationOptions
	{
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryFunc { get; set; }
		public Func<IPipeContext, string> MessageKeyFunc { get; set; }
	}

	public class MessageDeserializationMiddleware : Middleware
	{
		private readonly ISerializer _serializer;
		private readonly Func<IPipeContext, Type> _messageTypeFunc;
		private readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryFunc;
		private readonly Func<IPipeContext, string> _messageKeyFunc;

		public MessageDeserializationMiddleware(ISerializer serializer, MessageDeserializationOptions options = null)
		{
			_serializer = serializer;
			_messageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
			_deliveryFunc = options?.DeliveryFunc ?? (context => context.GetDeliveryEventArgs());
			_messageKeyFunc = options?.MessageKeyFunc ?? (context => PipeKey.Message);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var args = _deliveryFunc(context);
			var body = Encoding.UTF8.GetString(args.Body);
			var messageType = _messageTypeFunc(context);
			var messageKey = _messageKeyFunc(context);

			var message = _serializer.Deserialize(messageType, body);
			context.Properties.Add(messageKey, message);
			return Next.InvokeAsync(context);
		}
	}
}
