using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicPublishOptions
	{
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, string> ExchangeNameFunc { get; set; }
		public Func<IPipeContext, string> RoutingKeyFunc { get; set; }
		public Func<IPipeContext, bool> MandatoryFunc { get; set; }
		public Func<IPipeContext, IBasicProperties> BasicPropsFunc { get; set; }
		public Func<IPipeContext, byte[]> BodyFunc { get; set; }
	}

	public class BasicPublishMiddleware : Middleware
	{
		protected readonly IExclusiveLock Exclusive;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Func<IPipeContext, string> ExchangeNameFunc;
		protected Func<IPipeContext, string> RoutingKeyFunc;
		protected Func<IPipeContext, bool> MandatoryFunc;
		protected Func<IPipeContext, IBasicProperties> BasicPropsFunc;
		protected Func<IPipeContext, byte[]> BodyFunc;
		private ILogger _logger = LogManager.GetLogger<BasicPublishMiddleware>();

		public BasicPublishMiddleware(IExclusiveLock exclusive, BasicPublishOptions options = null)
		{
			Exclusive = exclusive;
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			ExchangeNameFunc = options?.ExchangeNameFunc ?? (c => c.GetBasicPublishConfiguration()?.ExchangeName);
			RoutingKeyFunc = options?.RoutingKeyFunc ?? (c => c.GetBasicPublishConfiguration()?.RoutingKey);
			MandatoryFunc = options?.MandatoryFunc ?? (c => c.GetBasicPublishConfiguration()?.Mandatory ?? false);
			BasicPropsFunc = options?.BasicPropsFunc ?? (c => c.GetBasicProperties());
			BodyFunc = options?.BodyFunc ?? (c => c.GetBasicPublishConfiguration()?.Body);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var channel = GetOrCreateChannel(context);
			var exchangeName = GetExchangeName(context);
			var routingKey = GetRoutingKey(context);
			var mandatory = GetMandatoryOptions(context);
			var basicProps = GetBasicProps(context);
			var body = GetMessageBody(context);

			_logger.LogInformation($"Performing basic publish with routing key {routingKey} on exchange {exchangeName}.");

			ExclusiveExecute(channel, c =>
					BasicPublish(
						channel: c,
						exchange: exchangeName,
						routingKey: routingKey,
						mandatory: mandatory,
						basicProps: basicProps,
						body: body,
						context: context
					), token
			);

			return Next.InvokeAsync(context, token);
		}

		protected virtual void BasicPublish(IModel channel, string exchange, string routingKey, bool mandatory, IBasicProperties basicProps, byte[] body, IPipeContext context)
		{
			channel.BasicPublish(
				exchange: exchange,
				routingKey: routingKey,
				mandatory: mandatory,
				basicProperties: basicProps,
				body: body
			);
		}

		protected virtual void ExclusiveExecute(IModel channel, Action<IModel> action, CancellationToken token)
		{
			Exclusive.Execute(channel, action, token);
		}

		protected virtual byte[] GetMessageBody(IPipeContext context)
		{
			var body = BodyFunc(context);
			if (body == null)
			{
				_logger.LogWarning("No body found in the Pipe context.");
			}
			return body;
		}

		protected virtual IBasicProperties GetBasicProps(IPipeContext context)
		{
			var props = BasicPropsFunc(context);
			if (props == null)
			{
				_logger.LogWarning("No basic properties found in the Pipe context.");
			}
			return props;
		}

		protected virtual bool GetMandatoryOptions(IPipeContext context)
		{
			return MandatoryFunc(context);
		}

		protected virtual string GetRoutingKey(IPipeContext context)
		{
			var routingKey =  RoutingKeyFunc(context);
			if (routingKey == null)
			{
				_logger.LogWarning("No routing key found in the Pipe context.");
			}
			return routingKey;
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			var exchange = ExchangeNameFunc(context);
			if (exchange == null)
			{
				_logger.LogWarning("No exchange name found in the Pipe context.");
			}
			return exchange;
		}

		protected virtual IModel GetOrCreateChannel(IPipeContext context)
		{
			var channel = ChannelFunc(context);
			if (channel == null)
			{
				_logger.LogWarning("No channel to perform publish found.");
			}
			return channel;
		}
	}
}
