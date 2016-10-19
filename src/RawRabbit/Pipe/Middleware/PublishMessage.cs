using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Pipe.Middleware
{
	public class PublishMessage : Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var exchange = context.GetExchangeConfiguration();
			var routingKey = context.GetRoutingKey();
			var basicProps = context.GetBasicProperties();
			var body = context.Get<byte[]>(PipeKey.MessageBytes);
			var channel = context.GetChannel();

			channel.BasicPublish(
				exchange: exchange.ExchangeName,
				routingKey: routingKey,
				basicProperties: basicProps,
				body: body,
				mandatory: false
				);

			return Next.InvokeAsync(context);
		}
	}
}
