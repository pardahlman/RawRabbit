using System.Threading.Tasks;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class ContextSerializationMiddleware : Middleware
	{
		private readonly IHeaderSerializer _serializer;

		public ContextSerializationMiddleware(IHeaderSerializer serializer)
		{
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var msgContext = context.GetMessageContext();
			var serialized = _serializer.Serialize(msgContext);
			context.Properties.Add(PipeKey.MessageContextBytes, serialized);
			return Next.InvokeAsync(context);
		}
	}
}
