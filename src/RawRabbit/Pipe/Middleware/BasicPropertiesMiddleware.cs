using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicPropertiesMiddleware : Middleware
	{
		private readonly IBasicPropertiesProvider _propertiesProvider;

		public BasicPropertiesMiddleware(IBasicPropertiesProvider propertiesProvider)
		{
			_propertiesProvider = propertiesProvider;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var msgType = context.GetMessageType();
			var msgContextHeader = context.Get<object>(PipeKey.MessageContextBytes);
			var operation = context.GetOperation();

			IBasicProperties props = null;
			switch (operation)
			{
					case Operation.Publish:
					var modifier = context.Get<Action<IBasicProperties>>(PipeKey.BasicPropertyModifier);
					props = _propertiesProvider.GetProperties(msgType, modifier + (p => p.Headers.Add(PropertyHeaders.Context, msgContextHeader)));
					break;
					case Operation.Subscribe:
						throw new NotImplementedException();
			}

			context.Properties.Add(PipeKey.BasicProperties, props);

			return Next.InvokeAsync(context);
		}
	}
}
