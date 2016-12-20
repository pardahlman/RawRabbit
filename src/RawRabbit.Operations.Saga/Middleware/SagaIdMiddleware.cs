using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class SagaIdOptions
	{
		public Func<IPipeContext, Func<object, Guid>> CorrelationFunc { get; set; }
		public Func<IPipeContext, object> MessageFunc { get; set; }
		public Func<IPipeContext, Guid> SagaIdFunc { get; set; }
	}

	public class SagaIdMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, Func<object, Guid>> CorrelationFunc;
		protected Func<IPipeContext, object> MessageFunc;
		protected Func<IPipeContext, Guid> SagaIdFunc;

		public SagaIdMiddleware(SagaIdOptions options = null)
		{
			CorrelationFunc = options?.CorrelationFunc ?? (context => context.GetIdCorrelationFunc());
			MessageFunc = options?.MessageFunc ?? (context => context.GetMessage());
			SagaIdFunc = options?.SagaIdFunc ?? (context => context.GetSagaId());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var corrFunc = GetCorrelationFunc(context);
			var msg = GetMessage(context);
			var id = GetSagaId(context, corrFunc, msg);
			context.Properties.TryAdd(SagaKey.SagaId, id);
			return Next.InvokeAsync(context, token);
		}

		protected virtual Func<object, Guid> GetCorrelationFunc(IPipeContext context)
		{
			return CorrelationFunc.Invoke(context);
		}

		protected virtual object GetMessage(IPipeContext context)
		{
			return MessageFunc.Invoke(context);
		}

		protected virtual Guid GetSagaId(IPipeContext context, Func<object, Guid> corrFunc, object message)
		{
			var fromComtext = SagaIdFunc?.Invoke(context);
			return fromComtext ?? corrFunc(message);
		}
	}
}
