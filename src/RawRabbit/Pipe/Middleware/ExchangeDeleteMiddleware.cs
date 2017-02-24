using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Pipe.Middleware
{
	public class ExchangeDeleteOptions
	{
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, string> ExchangeNameFunc { get; set; }
		public Func<IPipeContext, bool> IfUsedFunc { get; set; }
	}

	public class ExchangeDeleteMiddleware : Middleware
	{
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Func<IPipeContext, string> ExchangeNameFunc;
		protected Func<IPipeContext, bool> IfUsedFunc;

		public ExchangeDeleteMiddleware(ExchangeDeleteOptions options)
		{
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			ExchangeNameFunc = options?.ExchangeNameFunc ?? (context => string.Empty);
			IfUsedFunc = options?.IfUsedFunc ?? (context => false);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var channel = GetChannel(context);
			var exchangeName = GetExchangeName(context);
			var ifUsed = GetIfUsed(context);
			DeleteEchange(channel, exchangeName, ifUsed);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void DeleteEchange(IModel channel, string exchangeName, bool ifUsed)
		{
			channel.ExchangeDelete(exchangeName, ifUsed);
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc?.Invoke(context);
		}

		protected virtual string GetExchangeName(IPipeContext context)
		{
			return ExchangeNameFunc?.Invoke(context);
		}

		protected virtual bool GetIfUsed(IPipeContext context)
		{
			return IfUsedFunc.Invoke(context);
		}
	}
}
