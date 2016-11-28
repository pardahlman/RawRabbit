using System;
using System.Threading.Tasks;
using RabbitMQ.Client;

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
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Func<IPipeContext, string> ExchangeNameFunc;
		protected Func<IPipeContext, string> RoutingKeyFunc;
		protected Func<IPipeContext, bool> MandatoryFunc;
		protected Func<IPipeContext, IBasicProperties> BasicPropsFunc;
		protected Func<IPipeContext, byte[]> BodyFunc;

		public BasicPublishMiddleware(BasicPublishOptions options)
		{
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			ExchangeNameFunc = options?.ExchangeNameFunc ?? (c => c.GetBasicPublishConfiguration()?.ExchangeName);
			RoutingKeyFunc = options?.RoutingKeyFunc ?? (c => c.GetBasicPublishConfiguration()?.RoutingKey);
			MandatoryFunc = options?.MandatoryFunc ?? (c => c.GetBasicPublishConfiguration()?.Mandatory ?? false);
			BasicPropsFunc = options?.BasicPropsFunc ?? (c => c.GetBasicPublishConfiguration()?.BasicProperties);
			BodyFunc = options?.BodyFunc ?? (c => c.GetBasicPublishConfiguration()?.Body);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var channel = GetOrCreateChannel(context);
			var exchangeName = GetExchangeName(context);
			var routingKey = GetRoutingKey(context);
			var mandatory = GetMandatoryOptions(context);
			var basicProps = GetBasicProps(context);
			var body = GetMessageBondy(context);

			channel.BasicPublish(
				exchange: exchangeName,
				routingKey: routingKey,
				mandatory: mandatory,
				basicProperties: basicProps,
				body: body
			);

			return Next.InvokeAsync(context);
		}

		protected virtual byte[] GetMessageBondy(IPipeContext context)
		{
			return BodyFunc(context);
		}

		protected virtual IBasicProperties GetBasicProps(IPipeContext context)
		{
			return BasicPropsFunc(context);
		}

		protected virtual bool GetMandatoryOptions(IPipeContext context)
		{
			return MandatoryFunc(context);
		}

		protected virtual string GetRoutingKey(IPipeContext context)
		{
			return RoutingKeyFunc(context);
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			return ExchangeNameFunc(context);
		}

		protected virtual IModel GetOrCreateChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}
	}
}
