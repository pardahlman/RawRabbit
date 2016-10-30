using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class MessageDeserializationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly JsonSerializer _serializer;

		public MessageDeserializationMiddleware(JsonSerializer serializer)
		{
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var args = context.GetDeliveryEventArgs();
			var body = Encoding.UTF8.GetString(args.Body);
			var messageType = context.GetMessageType();

			object message;
			using (var jsonReader = new JsonTextReader(new StringReader(body)))
			{
				message = _serializer.Deserialize(jsonReader, messageType);
			}
			context.Properties.Add(PipeKey.Message, message);
			return Next.InvokeAsync(context);
		}
	}
}
