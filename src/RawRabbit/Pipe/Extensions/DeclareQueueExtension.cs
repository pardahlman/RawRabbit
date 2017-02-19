using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Advanced
{
	public static class DeclareQueueExtension
	{
		public static readonly Action<IPipeBuilder> DeclareQueueAction = pipe => pipe
			.Use<ConsumeConfigurationMiddleware>()
			.Use<QueueDeclareMiddleware>();

		public static async Task DeclareQueueAsync(this IBusClient client, QueueDeclaration declaration, CancellationToken ct = default(CancellationToken))
		{
			await client.InvokeAsync(DeclareQueueAction, ctx => ctx.Properties.Add(PipeKey.QueueDeclaration, declaration), ct);
		}

		public static async Task DeclareQueueAsync<TMessage>(this IBusClient client, CancellationToken ct = default(CancellationToken))
		{
			await client.InvokeAsync(DeclareQueueAction, ctx => ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage)), ct);
		}
	}
}
