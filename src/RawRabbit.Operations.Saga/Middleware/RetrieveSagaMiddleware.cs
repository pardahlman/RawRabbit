using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Repository;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class RetrieveSagaOptions
	{
		public Func<IPipeContext, Guid> SagaIdFunc { get; set; }
		public Func<IPipeContext, Type> SagaTypeFunc { get; set; }
		public Action<Model.Saga, IPipeContext> PostExecuteAction { get; set; }
	}

	public class RetrieveSagaMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISagaRepository _sagaRepo;
		protected Func<IPipeContext, Guid> SagaIdFunc;
		protected Func<IPipeContext, Type> SagaTypeFunc;
		protected Action<Model.Saga, IPipeContext> PostExecuteAction;

		public RetrieveSagaMiddleware(ISagaRepository sagaRepo, RetrieveSagaOptions options = null)
		{
			_sagaRepo = sagaRepo;
			SagaIdFunc = options?.SagaIdFunc ?? (context => context.Get(SagaKey.SagaId, Guid.NewGuid()));
			SagaTypeFunc = options?.SagaTypeFunc ?? (context => context.Get<Type>(SagaKey.SagaType));
			PostExecuteAction = options?.PostExecuteAction;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var id = GetSagaId(context);
			var sagaType = GetSagaType(context);
			var sagaTasks = GetSagaAsync(id, sagaType);
			return sagaTasks
				.ContinueWith(tSaga =>
				{
					context.Properties.TryAdd(SagaKey.Saga, tSaga.Result);
					PostExecuteAction?.Invoke(tSaga.Result, context);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}

		protected virtual Task<Model.Saga> GetSagaAsync(Guid id, Type sagaType)
		{
			return _sagaRepo.GetAsync(id, sagaType);
		}

		protected virtual Type GetSagaType(IPipeContext context)
		{
			return SagaTypeFunc(context);
		}

		protected virtual Guid GetSagaId(IPipeContext context)
		{
			return SagaIdFunc(context);
		}
	}
}
