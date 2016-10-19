using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class MessageSerializationMiddleware : Middleware
	{
		private readonly IMessageSerializer _serializer;

		public MessageSerializationMiddleware(IMessageSerializer serializer)
		{
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var body = _serializer.Serialize(message);
			context.Properties.Add(PipeKey.MessageBytes, body);
			return Next.InvokeAsync(context);
		}
	}
}
