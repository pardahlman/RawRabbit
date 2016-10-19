using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration;

namespace RawRabbit.Pipe.Middleware
{
	public class RoutingKeyMiddleware : Middleware
	{
		private readonly INamingConventions _conventions;
		private readonly RawRabbitConfiguration _config;

		public RoutingKeyMiddleware(INamingConventions conventions, RawRabbitConfiguration config)
		{
			_conventions = conventions;
			_config = config;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			string routingKey;
			var operation = context.GetOperation();

			if (operation == Operation.Unknown)
			{
				throw new Exception();
			}

			switch (operation)
			{
				case Operation.Publish:
					routingKey = GetPublishRoutingKey(context);
					break;
				case Operation.Subscribe:
					routingKey = GetSubscribeRoutingKey(context);
					break;
				default:
					routingKey = string.Empty;
					break;
			}

			context.Properties.Add(PipeKey.RoutingKey, routingKey);
			return Next.InvokeAsync(context);
		}

		private string GetSubscribeRoutingKey(IPipeContext context)
		{
			var msgType = context.GetMessageType();
			var routingKey = _conventions.RoutingKeyConvention(msgType);
			return _config.RouteWithGlobalId
				? $"{routingKey}.#"
				: routingKey;
		}

		private string GetPublishRoutingKey(IPipeContext context)
		{
			var msgType = context.GetMessageType();
			var msgId = context.GetGlobalMessageId();
			var routingKey = _conventions.RoutingKeyConvention(msgType);
			return _config.RouteWithGlobalId
				? $"{routingKey}.{msgId}"
				: routingKey;
		}
	}
}