using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Operations.Respond.Extensions;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ResponseSerializationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly JsonSerializer _serializer;

		public ResponseSerializationMiddleware(JsonSerializer serializer)
		{
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var response = context.GetResponseMessageType();

			string serialized;
			using (var sw = new StringWriter())
			{
				_serializer.Serialize(sw, response);
				serialized = sw.GetStringBuilder().ToString();
			}
			context.Properties.Add(RespondKey.SerializedResponse, serialized);
			return Next.InvokeAsync(context);
		}
	}
}
