using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Tools.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class DeleteExchangeExtension
	{
		private const string ExchangeName = "DeleteExchange:ExchangeName";
		private const string IfUsed = "DeleteExchange:IfUsed";

		public static readonly Action<IPipeBuilder> DeleteExchangePipe = builder => builder
			.Use<ExchangeDeclarationMiddleware>()
			.Use((context, func) =>
			{
				var cfg = context.GetExchangeDeclaration();
				if (cfg != null)
				{
					context.Properties.TryAdd(ExchangeName, cfg.Name);
				}
				return func();
			})
			.Use<PooledChannelMiddleware>()
			.Use<ExchangeDeleteMiddleware>(new ExchangeDeleteOptions
			{
				ExchangeNameFunc = context => context.Get<string>(ExchangeName),
				IfUsedFunc = context => context.Get<bool>(IfUsed)
			});

		public static Task DeleteExchangeAsync(this IBusClient client, string exchangeName, bool ifUsed = false)
		{
			return client.InvokeAsync(DeleteExchangePipe, context =>
			{
				context.Properties.Add(ExchangeName, exchangeName);
				context.Properties.Add(IfUsed, ifUsed);
			});
		}

		public static Task DeleteExchangeAsync<TMessage>(this IBusClient client, bool ifUsed = false)
		{
			return client.InvokeAsync(DeleteExchangePipe, context =>
			{
				context.Properties.Add(PipeKey.MessageType, typeof(TMessage));
				context.Properties.Add(IfUsed, ifUsed);
			});
		}
	}
}
