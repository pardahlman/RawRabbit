using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class UnbindQueueExtension
	{
		public static readonly Action<IPipeBuilder> UnbindQueueAction = pipe => pipe
			.Use<ConsumeConfigurationMiddleware>()
			.Use<QueueUnbindMiddleware>();

		public static Task UnbindQueueAsync(this IBusClient client, string queueName, string exchangeName, string routingKey, CancellationToken ct = default (CancellationToken))
		{
			return client.InvokeAsync(UnbindQueueAction, cfg =>
			{
				cfg.Properties.AddOrReplace(PipeKey.ConsumeConfiguration, new ConsumeConfiguration
				{
					QueueName = queueName,
					ExchangeName = exchangeName,
					RoutingKey = routingKey
				});
			}, ct);
		}

		public static Task UnbindQueueAsync<TMessage>(this IBusClient client, CancellationToken ct = default(CancellationToken))
		{
			return client.InvokeAsync(UnbindQueueAction, cfg =>
			{
				cfg.Properties.AddOrReplace(PipeKey.MessageType, typeof(TMessage));
			}, ct);
		}
	}
}
