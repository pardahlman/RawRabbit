using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Repository;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class TriggerMessageInvokationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISagaRepository _repo;

		public TriggerMessageInvokationMiddleware(ISagaRepository repo)
		{
			_repo = repo;
		}

		public override async Task InvokeAsync(IPipeContext context)
		{
			var triggerMsg = context.GetMessage();
			var invoker = context.GetTriggerInvoker();
			var sagaId = invoker.CorrelationFunc(triggerMsg);
			var sagaType = context.Get<Type>(SagaKey.SagaType);
			var saga = await _repo.GetAsync(sagaId, sagaType);
			await saga.TriggerAsync(invoker.Trigger, triggerMsg);
			await Next.InvokeAsync(context);
		}
	}
}