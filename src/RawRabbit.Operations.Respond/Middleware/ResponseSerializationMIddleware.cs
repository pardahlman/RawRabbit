using System.IO;
using System.Text;
using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Serialization;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ResponseSerializationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISerializer _serializer;

		public ResponseSerializationMiddleware(ISerializer serializer)
		{
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var response = context.Get<object>(RespondKey.ResponseMessage);

			var serialized = _serializer.Serialize(response);
			context.Properties.Add(RespondKey.SerializedResponse, serialized);
			return Next.InvokeAsync(context);
		}
	}
}
