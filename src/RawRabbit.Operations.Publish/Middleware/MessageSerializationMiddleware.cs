using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Serialization;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class MessageSerializationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISerializer _serializer;

		public MessageSerializationMiddleware(ISerializer serializer)
		{
			_serializer = serializer;
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			var message = context.GetMessage();
			var serialized = _serializer.Serialize(message);
			context.Properties.Add(PipeKey.SerializedMessage, serialized);
			return Next.InvokeAsync(context);
		}
	}
}
