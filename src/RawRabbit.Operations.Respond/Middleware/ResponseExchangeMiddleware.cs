using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ResponseExchangeMiddleware : Pipe.Middleware.Middleware
	{
		private readonly INamingConventions _conventions;

		public ResponseExchangeMiddleware(INamingConventions conventions)
		{
			_conventions = conventions;
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			var args = context.GetDeliveryEventArgs();
			throw new System.NotImplementedException();
		}
	}
}
