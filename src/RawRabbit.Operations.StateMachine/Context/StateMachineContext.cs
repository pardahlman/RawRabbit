using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Context
{
	public interface IStateMachineContext : IPipeContext { }

	public class StateMachineContext : PipeContext, IStateMachineContext
	{
		public StateMachineContext(IPipeContext context)
		{
			Properties = context?.Properties;
		}
	}
}
