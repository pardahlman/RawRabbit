using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageDeserializationMiddleware : Middleware
	{
		private readonly IMessageSerializer _serializer;

		public MessageDeserializationMiddleware(IMessageSerializer serializer)
		{
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var args = context.GetDeliveryEventArgs();
			var messageType = context.GetMessageType();

			var message = _serializer.Deserialize(args.Body, messageType);
			context.Properties.Add(PipeKey.Message, message);
			return Next.InvokeAsync(context);
		}
	}
}
