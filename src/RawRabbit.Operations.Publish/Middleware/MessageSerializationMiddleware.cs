using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class MessageSerializationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly JsonSerializer _serializer;

		public MessageSerializationMiddleware(JsonSerializer serializer)
		{
			_serializer = serializer;
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			var message = context.GetMessage();

			string serialized;
			using (var sw = new StringWriter())
			{
				_serializer.Serialize(sw, message);
				serialized = sw.GetStringBuilder().ToString();
			}
			context.Properties.Add(PipeKey.SerializedMessage, serialized);
			return Next.InvokeAsync(context);
		}
	}
}
