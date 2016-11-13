using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class HeaderDeserializationOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
		public Action<IPipeContext, object> ContextSaveAction { get; set; }
		public string HeaderKey { get; set; }
		public Type Type { get; set; }
	}

	public class HeaderDeserializationMiddleware : StagedMiddleware
	{
		private readonly ISerializer _serializer;
		private readonly string _headerKey;
		private readonly Func<IPipeContext, BasicDeliverEventArgs> _deliveryArgsFunc;
		private readonly Action<IPipeContext, object> _contextSaveAction;
		private readonly Type _headerType;

		public HeaderDeserializationMiddleware(ISerializer serializer, HeaderDeserializationOptions options = null)
		{
			_headerKey = options?.HeaderKey;
			_headerType = options?.Type ?? typeof(object);
			_deliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			_contextSaveAction = options?.ContextSaveAction ?? ((context, item) => context.Properties.TryAdd(_headerKey, item));
			_serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var args = _deliveryArgsFunc(context);
			if (args == null)
			{
				throw new ArgumentNullException(nameof(args));
			}
			object headerBytes;
			if (!args.BasicProperties.Headers.TryGetValue(_headerKey, out headerBytes))
			{
				return Next.InvokeAsync(context);
			}

			var serializedHeader = Encoding.UTF8.GetString((byte[])headerBytes);
			var deserializedHeader = _serializer.Deserialize(_headerType, serializedHeader);
			if (deserializedHeader == null)
			{
				throw new Exception();
			}
			_contextSaveAction(context, deserializedHeader);
			return Next.InvokeAsync(context);
		}

		public override string StageMarker => Pipe.StageMarker.MessageRecieved;
	}
}
