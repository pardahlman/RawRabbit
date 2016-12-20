using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ReplyToExtractionMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var args = context.GetDeliveryEventArgs();
			var replyTo = args.BasicProperties.ReplyToAddress ?? new PublicationAddress(ExchangeType.Direct, string.Empty, args.BasicProperties.ReplyTo);
			args.BasicProperties.ReplyTo = replyTo.RoutingKey;
			context.Properties.Add(RespondKey.PublicationAddress, replyTo);
			return Next.InvokeAsync(context, token);
		}
	}
}
