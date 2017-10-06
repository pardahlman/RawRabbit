using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.Tools.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class DeclareExchangeExtension
	{
		public static readonly Action<IPipeBuilder> DeclareQueueAction = pipe => pipe
			.Use<ExchangeDeclarationMiddleware>()
			.Use<ExchangeDeclareMiddleware>();

		public static Task DeclareExchangeAsync<TMessage>(this IBusClient client, CancellationToken ct = default(CancellationToken))
		{
			return client.InvokeAsync(DeclareQueueAction, context => context.Properties.TryAdd(PipeKey.MessageType, typeof(TMessage)), ct);
		}
	}
}
