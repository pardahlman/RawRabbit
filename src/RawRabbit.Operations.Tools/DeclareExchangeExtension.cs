using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Operations.Tools.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class DeclareExchangeExtension
	{
		public static readonly Action<IPipeBuilder> DeclareExchangeAction = pipe => pipe
			.Use<ExchangeDeclarationMiddleware>()
			.Use<ExchangeDeclareMiddleware>();
		
		public static Task DeclareExchangeAsync(this IBusClient client, ExchangeDeclaration declaration, CancellationToken ct = default(CancellationToken))
		{
			return client.InvokeAsync(DeclareExchangeAction, ctx => ctx.Properties.Add(PipeKey.ExchangeDeclaration, declaration), ct);
		}

		public static Task DeclareExchangeAsync<TMessage>(this IBusClient client, CancellationToken ct = default(CancellationToken))
		{
			return client.InvokeAsync(DeclareExchangeAction, context => context.Properties.TryAdd(PipeKey.MessageType, typeof(TMessage)), ct);
		}
	}
}
