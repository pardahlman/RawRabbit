using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Operations.Saga.Repository;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class TriggerMessageInvokationOptions
	{
		public Func<IPipeContext, Model.Saga> SagaFunc { get; set; }
		public Func<IPipeContext, object> MessageFunc { get; set; }
		public Func<IPipeContext, Type> SagaTypeFunc { get; set; }
		public Func<IPipeContext, TriggerInvoker> TriggerInvokerFunc { get; set; }
	}
	public class TriggerMessageInvokationMiddleware : Pipe.Middleware.Middleware
	{
		protected readonly ISagaRepository Repo;
		private readonly IExclusiveLockRepo _lockRepo;
		protected Func<IPipeContext, object> MessageFunc;
		protected Func<IPipeContext, TriggerInvoker> TriggerInvokerFunc;
		protected Func<IPipeContext, Type> SagaTypeFunc;
		protected Func<IPipeContext, Model.Saga> SagaFunc;

		public TriggerMessageInvokationMiddleware(ISagaRepository repo, IExclusiveLockRepo lockRepo, TriggerMessageInvokationOptions options = null)
		{
			Repo = repo;
			_lockRepo = lockRepo;
			MessageFunc = options?.MessageFunc ?? (context => context.GetMessage());
			TriggerInvokerFunc = options?.TriggerInvokerFunc ?? (context => context.GetTriggerInvoker());
			SagaTypeFunc = options?.SagaTypeFunc ?? (context => context.Get<Type>(SagaKey.SagaType));
			SagaFunc = options?.SagaFunc;
		}

		public override async Task InvokeAsync(IPipeContext context)
		{
			var triggerMsg = GetMessage(context);
			var invoker = GetTriggerInvoker(context);
			var sagaId = GetSagaId(context, triggerMsg, invoker);
			var sagaType = GetSagaType(context);

			await _lockRepo.ExecuteAsync(sagaId, async () =>
			{
				var saga = await GetSagaAsync(context, sagaId, sagaType);
				await saga.TriggerAsync(invoker.Trigger, triggerMsg);
			});
			

			await Next.InvokeAsync(context);
		}

		protected virtual object GetMessage(IPipeContext context)
		{
			return MessageFunc(context);
		}

		protected virtual TriggerInvoker GetTriggerInvoker(IPipeContext context)
		{
			return TriggerInvokerFunc(context);
		}

		protected virtual Type GetSagaType(IPipeContext context)
		{
			return SagaTypeFunc(context);
		}

		protected virtual Guid GetSagaId(IPipeContext context, object message, TriggerInvoker invoker)
		{
			return invoker.CorrelationFunc(message);
		}

		protected virtual Task<Model.Saga> GetSagaAsync(IPipeContext context, Guid sagaId, Type sagaType)
		{
			var saga = SagaFunc(context);
			return saga != null
				? Task.FromResult(saga)
				: Repo.GetAsync(sagaId, sagaType);
		}
	}
}