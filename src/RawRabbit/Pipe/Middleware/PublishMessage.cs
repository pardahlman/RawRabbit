using System.Text;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class PublishMessage : Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var exchange = context.GetExchangeName();
			var routingKey = context.GetRoutingKey();
			var basicProps = context.GetBasicProperties();
			var mandatory = context.GetMandatoryPublishFlag();
			var body = context.Get<string>(PipeKey.SerializedMessage);
			var channel = context.GetChannel();

			channel.BasicPublish(
				exchange: exchange,
				routingKey: routingKey,
				basicProperties: basicProps,
				body: Encoding.UTF8.GetBytes(body),
				mandatory: mandatory
				);

			return Next.InvokeAsync(context);
		}
	}
}
