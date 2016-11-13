using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class HeaderSerializationOptions
	{
		public Func<IPipeContext, IBasicProperties> BasicPropsFunc { get; set; }
		public Func<IPipeContext, object> RetrieveItemFunc { get; set; }
		public Func<IPipeContext, object> CreateItemFunc { get; set; }
		public string HeaderKey { get; set; }
	}

	public class HeaderSerializationMiddleware : StagedMiddleware
	{
		private readonly ISerializer _serializer;
		private readonly Func<IPipeContext, IBasicProperties> _basicPropsFunc;
		private readonly string _headerKey;
		private readonly Func<IPipeContext, object> _retrieveItemFunc;
		private readonly Func<IPipeContext, object> _createItemFunc;

		public HeaderSerializationMiddleware(ISerializer serializer, HeaderSerializationOptions options = null)
		{
			_serializer = serializer;
			_basicPropsFunc = options?.BasicPropsFunc ?? (context => context.GetBasicProperties());
			_headerKey = options?.HeaderKey;
			_retrieveItemFunc = options?.RetrieveItemFunc;
			_createItemFunc = options?.CreateItemFunc;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var properties = _basicPropsFunc(context);
			if (properties.Headers.ContainsKey(_headerKey))
			{
				return Next.InvokeAsync(context);
			}

			var item = _retrieveItemFunc(context) ?? _createItemFunc(context);

			var serializedItem = _serializer.Serialize(item);
			properties.Headers.Add(PropertyHeaders.Context, serializedItem);
			return Next.InvokeAsync(context);
		}

		public override string StageMarker => Pipe.StageMarker.BasicPropertiesCreated;
	}
}
