using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Pipe.Middleware
{
	public class PublishOptions
	{
		public Func<IPipeContext, string> ExchangeFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
		public Func<IPipeContext, string> BodyFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
	}
	public class PublishMessage : Middleware
	{
		private readonly Func<IPipeContext, string> _exchangeFunc;
		private readonly Func<IPipeContext, string> _routingKeyFunc;
		private readonly Func<IPipeContext, string> _bodyFunc;
		private readonly Func<IPipeContext, IModel> _channelFunc;

		public PublishMessage(PublishOptions options = null)
		{
			_exchangeFunc = options?.ExchangeFunc ?? (context => context.GetPublishConfiguration()?.Exchange.ExchangeName);
			_routingKeyFunc = options?.RoutingKeyFunc ?? (context =>context.GetPublishConfiguration()?.RoutingKey);
			_bodyFunc = options?.BodyFunc ?? (context => context.Get<string>(PipeKey.SerializedMessage));
			_channelFunc = options?.ChannelFunc ?? (context =>context.GetTransientChannel());
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var exchange = _exchangeFunc(context);
			var routingKey = _routingKeyFunc(context);
			var basicProps = context.GetBasicProperties();
			var mandatory = context.GetMandatoryPublishFlag();
			var body = _bodyFunc(context);
			var channel = _channelFunc(context);

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
