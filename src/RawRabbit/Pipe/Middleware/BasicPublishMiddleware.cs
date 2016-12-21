using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;

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

			ExclusiveExecute(channel, c => c.BasicPublish(
				exchange: exchangeName,
				routingKey: routingKey,
				mandatory: mandatory,
				basicProperties: basicProps,
				body: body
			), token);

			return Next.InvokeAsync(context, token);
		}

		protected virtual void ExclusiveExecute(IModel channel, Action<IModel> action, CancellationToken token)
		{
			Exclusive.Execute(channel, action, token);
		}

		protected virtual byte[] GetMessageBody(IPipeContext context)
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
