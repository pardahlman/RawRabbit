using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe.Extensions
{
	public static class DeleteQueueExtension
	{
		private const string QueueName = "DeleteQueue:QueueName";

		public static readonly Action<IPipeBuilder> DeletePipe = builder => builder
			.Use<ConsumeConfigurationMiddleware>()
			.Use((context, func) =>
			{
				var consumeCfg = context.GetConsumeConfiguration();
				if (consumeCfg != null)
				{
					context.Properties.TryAdd(QueueName, consumeCfg.QueueName);
				}
				return func();
			})
			.Use<TransientChannelMiddleware>()
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
