using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Repository;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class ExclusiveLockMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IExclusiveLockRepo _repo;

		public ExclusiveLockMiddleware(IExclusiveLockRepo repo)
		{
			_repo = repo;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			return _repo.ExecuteAsync(context.Get<Guid>(SagaKey.SagaId), () => Next.InvokeAsync(context));
		}
	}
}
