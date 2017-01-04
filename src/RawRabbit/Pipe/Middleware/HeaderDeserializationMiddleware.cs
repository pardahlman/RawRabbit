using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Logging;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class HeaderDeserializationOptions
	{
		public Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc { get; set; }
		public Func<IPipeContext, string> HeaderKeyFunc { get; set; }
		public Func<IPipeContext, Type> HeaderTypeFunc { get; set; }
		public Action<IPipeContext, object> ContextSaveAction { get; set; }
	}

	public class HeaderDeserializationMiddleware : StagedMiddleware
	{
		protected readonly ISerializer Serializer;
		protected Func<IPipeContext, BasicDeliverEventArgs> DeliveryArgsFunc;
		protected Action<IPipeContext, object> ContextSaveAction;
		protected Func<IPipeContext, string> HeaderKeyFunc;
		protected Func<IPipeContext, Type> HeaderTypeFunc;
		private readonly ILogger _logger = LogManager.GetLogger<HeaderDeserializationMiddleware>();

		public HeaderDeserializationMiddleware(ISerializer serializer, HeaderDeserializationOptions options = null)
		{
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			HeaderKeyFunc = options?.HeaderKeyFunc;
			ContextSaveAction = options?.ContextSaveAction ?? ((context, item) => context.Properties.TryAdd(HeaderKeyFunc(context), item));
			HeaderTypeFunc = options?.HeaderTypeFunc ?? (context =>typeof(object)) ;
			Serializer = serializer;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var headerObject = GetHeaderObject(context);
			SaveInContext(context, headerObject);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void SaveInContext(IPipeContext context, object headerValue)
		{
			ContextSaveAction?.Invoke(context, headerValue);
		}

		protected virtual object GetHeaderObject(IPipeContext context)
		{
			var serialized = GetSerializedHeader(context);
			var type = GetHeaderType(context);
			return Serializer.Deserialize(type, serialized);
		}

		protected virtual string GetSerializedHeader(IPipeContext context)
		{
			var headerBytes = GetHeaderBytes(context);
			return Encoding.UTF8.GetString(headerBytes ?? new byte[0]);
		}

		protected virtual byte[] GetHeaderBytes(IPipeContext context)
		{
			var headerKey = GetHeaderKey(context);
			var args = GetDeliveryArgs(context);
			if (string.IsNullOrEmpty(headerKey))
			{
				return null;
			}
			if (args == null)
			{
				return null;
			}

			object headerBytes;
			return args.BasicProperties.Headers.TryGetValue(headerKey, out headerBytes)
				? headerBytes as byte[]
				: null;
		}

		protected virtual BasicDeliverEventArgs GetDeliveryArgs(IPipeContext context)
		{
			var args = DeliveryArgsFunc(context);
			if (args == null)
			{
				_logger.LogWarning("Unable to extract delivery args from Pipe Context.");
			}
			return args;
		}

		protected virtual string GetHeaderKey(IPipeContext context)
		{
			var key = HeaderKeyFunc(context);
			if (key == null)
			{
				_logger.LogWarning("Unable to extract header key from Pipe context.");
			}
			return key;
		}

		protected virtual Type GetHeaderType(IPipeContext context)
		{
			var type = HeaderTypeFunc(context);
			if (type == null)
			{
				_logger.LogWarning("Unable to extract header type from Pipe context.");
			}
			return type;
		}

		public override string StageMarker => Pipe.StageMarker.MessageRecieved;
	}
}
