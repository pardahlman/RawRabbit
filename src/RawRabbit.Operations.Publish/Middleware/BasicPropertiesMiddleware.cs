using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class BasicPropertiesMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IBasicPropertiesProvider _provider;

		public BasicPropertiesMiddleware(IBasicPropertiesProvider provider)
		{
			_provider = provider;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var messageType = context.GetMessageType();
			var modifier = context.Get<Action<IBasicProperties>>(PipeKey.BasicPropertyModifier);
			var props = _provider.GetProperties(messageType, modifier);
			context.Properties.Add(PipeKey.BasicProperties, props);
			return Next.InvokeAsync(context);
		}
	}
}
