using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
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
			var responseType = context.GetResponseMessageType();
			var args = context.GetDeliveryEventArgs();
			var properties = _provider.GetProperties(responseType, props => props.CorrelationId = args.BasicProperties.CorrelationId);
			context.Properties.Add(PipeKey.BasicProperties, properties);
			return Next.InvokeAsync(context);
		}
	}
}
