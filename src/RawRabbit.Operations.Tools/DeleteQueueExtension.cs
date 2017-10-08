using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Tools.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class DeleteQueueExtension
	{
		private const string QueueName = "DeleteQueue:QueueName";

		public static readonly Action<IPipeBuilder> DeletePipe = builder => builder
			.Use<QueueDeclarationMiddleware>()
			.Use((context, func) =>
			{
				var consumeCfg = context.GetQueueDeclaration();
				if (consumeCfg != null)
				{
					context.Properties.TryAdd(QueueName, consumeCfg.Name);
				}
				return func();
			})
			.Use<PooledChannelMiddleware>()
			.Use<QueueDeleteMiddleware>(new QueueDeleteOptions
			{
				QueueNameFunc = context => context.Get<string>(QueueName)
			});

		public static Task DeleteQueueAsync(this IBusClient client, string queueName)
		{
			return client.InvokeAsync(DeletePipe, ctx => ctx.Properties.Add(QueueName, queueName));
		}

		public static Task DeleteQueueAsync<TMessage>(this IBusClient client)
		{
			return client.InvokeAsync(DeletePipe, ctx => ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage)));
		}
	}
}
