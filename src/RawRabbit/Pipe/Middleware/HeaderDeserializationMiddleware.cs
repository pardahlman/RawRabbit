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
		private readonly ILog _logger = LogProvider.For<HeaderDeserializationMiddleware>();

		public HeaderDeserializationMiddleware(ISerializer serializer, HeaderDeserializationOptions options = null)
		{
			DeliveryArgsFunc = options?.DeliveryArgsFunc ?? (context => context.GetDeliveryEventArgs());
			HeaderKeyFunc = options?.HeaderKeyFunc;
			ContextSaveAction = options?.ContextSaveAction ?? ((context, item) => context.Properties.TryAdd(HeaderKeyFunc(context), item));
			HeaderTypeFunc = options?.HeaderTypeFunc ?? (context =>typeof(object)) ;
			Serializer = serializer;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var headerObject = GetHeaderObject(context);
			if (headerObject != null)
			{
				SaveInContext(context, headerObject);
			}
			await Next.InvokeAsync(context, token);
		}

		protected virtual void SaveInContext(IPipeContext context, object headerValue)
		{
			ContextSaveAction?.Invoke(context, headerValue);
		}

		protected virtual object GetHeaderObject(IPipeContext context)
		{
			var bytes = GetHeaderBytes(context);
			if (bytes == null)
			{
				return null;
			}
			var type = GetHeaderType(context);
			return Serializer.Deserialize(type, bytes);
		}

		protected virtual byte[] GetHeaderBytes(IPipeContext context)
		{
			var headerKey = GetHeaderKey(context);
			var args = GetDeliveryArgs(context);
			if (string.IsNullOrEmpty(headerKey))
			{
				_logger.Debug("Key {headerKey} not found.", headerKey);
				return null;
			}
			if (args == null)
			{
				_logger.Debug("DeliveryEventArgs not found.");
				return null;
			}
			if (args.BasicProperties.Headers == null || !args.BasicProperties.Headers.ContainsKey(headerKey))
			{
				_logger.Info("BasicProperties Header does not contain {headerKey}", headerKey);
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
				_logger.Warn("Unable to extract delivery args from Pipe Context.");
			}
			return args;
		}

		protected virtual string GetHeaderKey(IPipeContext context)
		{
			var key = HeaderKeyFunc(context);
			if (key == null)
			{
				_logger.Warn("Unable to extract header key from Pipe context.");
			}
			else
			{
				_logger.Debug("Trying to extract {headerKey} from header", key);
			}
			return key;
		}

		protected virtual Type GetHeaderType(IPipeContext context)
		{
			var type = HeaderTypeFunc(context);
			if (type == null)
			{
				_logger.Warn("Unable to extract header type from Pipe context.");
			}
			else
			{
				_logger.Debug("Header type extracted: '{headerType}'", type.Name);
			}
			return type;
		}

		public override string StageMarker => Pipe.StageMarker.MessageReceived;
	}
}
