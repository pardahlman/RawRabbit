using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class ModelIdOptions
	{
		public Func<IPipeContext, Func<object, Guid>> CorrelationFunc { get; set; }
		public Func<IPipeContext, object> MessageFunc { get; set; }
		public Func<IPipeContext, Guid> ModelIdFunc { get; set; }
	}

	public class ModelIdMiddleware : StagedMiddleware
	{
		protected Func<IPipeContext, Func<object, Guid>> CorrelationFunc;
		protected Func<IPipeContext, object> MessageFunc;
		protected Func<IPipeContext, Guid> ModelIdFunc;

		public override string StageMarker => Pipe.StageMarker.MessageRecieved;

		public ModelIdMiddleware(ModelIdOptions options = null)
		{
			CorrelationFunc = options?.CorrelationFunc ?? (context => context.GetIdCorrelationFunc());
			MessageFunc = options?.MessageFunc ?? (context => context.GetMessage());
			ModelIdFunc = options?.ModelIdFunc ?? (context => context.GetModelId());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var corrFunc = GetCorrelationFunc(context);
			var msg = GetMessage(context);
			var id = GetModelId(context, corrFunc, msg);
			context.Properties.TryAdd(StateMachineKey.ModelId, id);
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

		protected virtual Guid GetModelId(IPipeContext context, Func<object, Guid> corrFunc, object message)
		{
			var fromComtext = ModelIdFunc?.Invoke(context);
			return fromComtext ?? corrFunc(message);
		}
	}
}
