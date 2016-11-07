using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Queue;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class BasicPropertiesMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IBasicPropertiesProvider _provider;
		private readonly RawRabbitConfiguration _config;

		public BasicPropertiesMiddleware(IBasicPropertiesProvider provider, RawRabbitConfiguration config)
		{
			_provider = provider;
			_config = config;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var responseType = context.GetResponseMessageType();
			var correlationId = context.GetCorrelationId();
			var cfg = context.GetResponseConfiguration();

			var props = _provider.GetProperties(responseType, p =>
			{
				if (cfg.Queue.IsDirectReplyTo())
				{
					p.ReplyTo = cfg.Queue.QueueName;
				}
				else
				{
					p.ReplyToAddress = new PublicationAddress(cfg.Exchange.ExchangeType, cfg.Exchange.ExchangeName, cfg.RoutingKey);
				}

				p.CorrelationId = correlationId;
				p.Expiration = _config.RequestTimeout.TotalMilliseconds.ToString();
			});

			context.Properties.Add(PipeKey.BasicProperties, props);
			return Next.InvokeAsync(context);
		}
	}
}
