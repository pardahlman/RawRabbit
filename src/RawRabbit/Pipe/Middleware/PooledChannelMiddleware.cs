using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel;

namespace RawRabbit.Pipe.Middleware
{
	public class PooledChannelOptions
	{
		public Func<IPipeContext, string> PoolNameFunc { get; set; }
		public Action<IPipeContext, IModel> SaveInContextAction { get; set; }
	}

	public class PooledChannelMiddleware : Middleware
	{
		protected readonly IChannelPoolFactory PoolFactory;
		protected readonly Func<IPipeContext, string> PoolNameFunc;
		protected readonly Action<IPipeContext, IModel> SaveInContextAction;

		public PooledChannelMiddleware(IChannelPoolFactory poolFactory, PooledChannelOptions options = null)
		{
			PoolFactory = poolFactory;
			PoolNameFunc = options?.PoolNameFunc;
			SaveInContextAction = options?.SaveInContextAction ?? ((ctx, value) =>ctx.Properties.TryAdd(PipeKey.TransientChannel, value));
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var channel = await GetChannelAsync(context, token);
			SaveInContext(context, channel);
			await Next.InvokeAsync(context, token);
		}

		protected virtual string GetChannelPoolName(IPipeContext context)
		{
			return PoolNameFunc?.Invoke(context);
		}

		protected virtual IChannelPool GetChannelPool(IPipeContext context)
		{
			var poolName = GetChannelPoolName(context);
			return PoolFactory.GetChannelPool(poolName);
		}

		protected virtual Task<IModel> GetChannelAsync(IPipeContext context, CancellationToken ct)
		{
			var channelPool = GetChannelPool(context);
			return channelPool.GetAsync(ct);
		}

		protected virtual void SaveInContext(IPipeContext context, IModel channel)
		{
			SaveInContextAction?.Invoke(context, channel);
		}
	}
}
