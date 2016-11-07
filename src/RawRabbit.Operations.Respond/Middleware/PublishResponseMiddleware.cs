using System.Text;
using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class PublishResponseMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var body = context.Get<string>(RespondKey.SerializedResponse);
			var channel = context.GetTransientChannel();
			var replyTo = context.GetPublicationAddress();
			var basicProps = context.GetBasicProperties();

			channel.BasicPublish(
				exchange: replyTo.ExchangeName,
				routingKey: replyTo.RoutingKey,
				mandatory: true,
				basicProperties: basicProps,
				body: Encoding.UTF8.GetBytes(body)
			);
			return Next.InvokeAsync(context);
		}
	}
}
