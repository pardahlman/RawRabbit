using System.Text;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Serialization;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class RequestDeserializationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISerializer _serializer;

		public RequestDeserializationMiddleware(ISerializer serializer)
		{
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var args = context.GetDeliveryEventArgs();
			var body = Encoding.UTF8.GetString(args.Body);
			var messageType = context.GetMessageType();

			var message = _serializer.Deserialize(messageType, body);
			context.Properties.Add(PipeKey.Message, message);
			return Next.InvokeAsync(context);
		}
	}
}
