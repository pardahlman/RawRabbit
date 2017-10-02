using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Pipe.Middleware
{
	public class QueueDeleteOptions
	{
		public Func<IPipeContext, string> QueueNameFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> IfUnusedFunc { get; set; }
		public Func<IPipeContext, bool> IfEmptyFunc { get; set; }
	}

	public class QueueDeleteMiddleware : Middleware
	{
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Func<IPipeContext, string> QueueNameFunc;
		protected Func<IPipeContext, bool> IfUnusedFunc;
		protected Func<IPipeContext, bool> IfEmptyFunc;

		public QueueDeleteMiddleware(QueueDeleteOptions options = null)
		{
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			QueueNameFunc = options?.QueueNameFunc ?? (context => string.Empty);
			IfUnusedFunc = options?.IfUnusedFunc ?? (context => false);
			IfEmptyFunc = options?.IfEmptyFunc ?? (context => false);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var channel = GetChannel(context);
			var queueName = GetQueueName(context);
			var ifEmpty = GetIfEmpty(context);
			var ifUnused = GetIfUnused(context);
			await DeleteQueueAsync(channel, queueName, ifUnused, ifEmpty);
			await Next.InvokeAsync(context, token);
		}

		private Task DeleteQueueAsync(IModel channel, string queueName, bool ifUnused, bool ifEmpty)
		{
			channel?.QueueDelete(queueName, ifUnused, ifEmpty);
			return Task.FromResult(true);
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			return QueueNameFunc(context);
		}

		protected virtual bool GetIfUnused(IPipeContext context)
		{
			return IfUnusedFunc(context);
		}

		protected virtual bool GetIfEmpty(IPipeContext context)
		{
			return IfEmptyFunc(context);
		}
	}
}
