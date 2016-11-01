using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Extensions;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class PublishResponseMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var body = context.Get<string>(RespondKey.SerializedResponse);
			var channel = context.GetChannel();
			var args = context.GetDeliveryEventArgs();

			//channel.BasicPublish();
			throw new System.NotImplementedException();
		}
	}
}
