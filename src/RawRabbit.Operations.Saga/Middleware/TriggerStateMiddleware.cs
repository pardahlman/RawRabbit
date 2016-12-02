using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Repository;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class TriggerStateMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISagaRepository _repo;

		public TriggerStateMiddleware(ISagaRepository repo)
		{
			_repo = repo;
		}

		public override async Task InvokeAsync(IPipeContext context)
		{
			var sagaType = context.Get<Type>(SagaKey.SagaType);
			var invoker = context.GetTriggerInvoker();
			var sagaId = new Guid("6B3B099D-35BF-436D-A051-0D5671DA6D25");
			var saga = await _repo.GetAsync(sagaId, sagaType);
			await saga.TriggerAsync(invoker.Trigger, context);
			await Next.InvokeAsync(context);
		}
	}
}