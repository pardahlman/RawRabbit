using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class ModelIdOptions
	{
		public Func<IPipeContext, Func<object[], Guid>> CorrelationFunc { get; set; }
		public Func<IPipeContext, Guid> ModelIdFunc { get; set; }
		public Func<IPipeContext, object[]> CorrelationArgsFunc { get; set; }
	}

	public class ModelIdMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, Func<object[], Guid>> CorrelationFunc;
		protected Func<IPipeContext, object[]> CorrelationArgsFunc;
		protected Func<IPipeContext, Guid> ModelIdFunc;

		public ModelIdMiddleware(ModelIdOptions options = null)
		{
			CorrelationFunc = options?.CorrelationFunc ?? (context => context.GetIdCorrelationFunc());
			CorrelationArgsFunc = options?.CorrelationArgsFunc ?? (context => context.GetLazyIdCorrelationArgs());
			ModelIdFunc = options?.ModelIdFunc ?? (context => context.GetModelId());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var corrFunc = GetCorrelationFunc(context);
			var corrArgs = GetCorrelationArgs(context);
			var id = GetModelId(context, corrFunc, corrArgs);
			context.Properties.TryAdd(StateMachineKey.ModelId, id);
			await Next.InvokeAsync(context, token);
		}

		protected virtual Func<object[], Guid> GetCorrelationFunc(IPipeContext context)
		{
			return CorrelationFunc.Invoke(context);
		}

		protected virtual object[] GetCorrelationArgs(IPipeContext context)
		{
			return CorrelationArgsFunc?.Invoke(context);
		}

		protected virtual Guid GetModelId(IPipeContext context, Func<object[], Guid> corrFunc, object[] args)
		{
			var fromContext = ModelIdFunc.Invoke(context);
			return fromContext != Guid.Empty ? fromContext : corrFunc(args);
		}
	}
}
