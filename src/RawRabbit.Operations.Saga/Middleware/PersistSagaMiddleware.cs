using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Repository;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class PersistSagaMiddleware : Pipe.Middleware.Middleware
	{
		private readonly ISagaRepository _sagaRepo;

		public PersistSagaMiddleware(ISagaRepository sagaRepo)
		{
			_sagaRepo = sagaRepo;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var saga = context.GetSaga();
			return _sagaRepo
				.UpdateAsync(saga)
				.ContinueWith(t => Next.InvokeAsync(context, token), token)
				.Unwrap();
		}
	}
}
