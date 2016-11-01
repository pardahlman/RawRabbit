using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ReplyToExtractionMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var args = context.GetDeliveryEventArgs();
			var replyTo = args.BasicProperties.ReplyToAddress ?? new PublicationAddress(ExchangeType.Direct, string.Empty, args.BasicProperties.ReplyTo);

			return Next.InvokeAsync(context);
		}
	}
}
