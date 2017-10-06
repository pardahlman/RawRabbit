using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Tools.Middleware
{
	public class ExchangeDeclarationOptions
	{
		public Func<IPipeContext, ExchangeDeclaration> ExchangeDeclarationFunc { get; internal set; }
		public Action<IPipeContext, ExchangeDeclaration> SaveToContextAction { get; internal set; }
	}

	public class ExchangeDeclarationMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly IExchangeDeclarationFactory CfgFactory;
		protected Func<IPipeContext, ExchangeDeclaration> ExchangeDeclarationFunc;
		protected Action<IPipeContext, ExchangeDeclaration> SaveToContextAction;

		public ExchangeDeclarationMiddleware(IExchangeDeclarationFactory cfgFactory, ExchangeDeclarationOptions options = null)
		{
			CfgFactory = cfgFactory;
			ExchangeDeclarationFunc = options?.ExchangeDeclarationFunc ?? (ctx => ctx.GetExchangeDeclaration());
			SaveToContextAction = options?.SaveToContextAction ?? ((ctx, d) => ctx.Properties.TryAdd(PipeKey.ExchangeDeclaration, d)); 
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			var queueDeclaration = GetQueueDeclaration(context);
			SaveToContext(context, queueDeclaration);
			return Next.InvokeAsync(context, token);
		}

		protected virtual ExchangeDeclaration GetQueueDeclaration(IPipeContext context)
		{
			var declaration = ExchangeDeclarationFunc?.Invoke(context);
			if (declaration != null)
			{
				return declaration;
			}
			var messageType = context.GetMessageType();
			return CfgFactory.Create(messageType);
		}

		protected virtual void SaveToContext(IPipeContext context, ExchangeDeclaration declaration)
		{
			SaveToContextAction?.Invoke(context, declaration);
		}
	}
}
