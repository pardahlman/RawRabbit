using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class PersistModelMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IStateMachineActivator _stateMachineRepo;

		public PersistModelMiddleware(IStateMachineActivator stateMachineRepo)
		{
			_stateMachineRepo = stateMachineRepo;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var machine = context.GetStateMachine();
			await _stateMachineRepo.PersistAsync(machine);
			await Next.InvokeAsync(context, token);
		}
	}
}
