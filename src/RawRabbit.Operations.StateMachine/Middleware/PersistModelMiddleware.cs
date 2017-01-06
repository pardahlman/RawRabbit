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

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var machine = context.GetStateMachine();
			return _stateMachineRepo
				.PersistAsync(machine)
				.ContinueWith(t => Next.InvokeAsync(context, token), token)
				.Unwrap();
		}
	}
}
