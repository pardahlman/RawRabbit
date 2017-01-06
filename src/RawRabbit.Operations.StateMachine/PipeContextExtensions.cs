using System;
using System.Collections.Generic;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine
{
	public static class PipeContextExtensions
	{
		public static StateMachineBase GetStateMachine(this IPipeContext context)
		{
			return context.Get<StateMachineBase>(StateMachineKey.Machine);
		}

		public static Guid GetModelId(this IPipeContext context)
		{
			return context.Get<Guid>(StateMachineKey.ModelId);
		}

		public static List<TriggerPipeOptions> GetTriggerPipeOptions(this IPipeContext context)
		{
			return context.Get<List<TriggerPipeOptions>>(StateMachineKey.TriggerPipeOptions);
		}

		public static Action<IPipeContext> GetContextAction(this IPipeContext context)
		{
			return context.Get<Action<IPipeContext>>(StateMachineKey.ContextAction);
		}

		public static Action<IPipeBuilder> GetPipeBuilderAction(this IPipeContext context)
		{
			return context.Get<Action<IPipeBuilder>>(StateMachineKey.PipeBuilderAction);
		}

		public static Func<object, Guid> GetIdCorrelationFunc(this IPipeContext context)
		{
			return context.Get<Func<object, Guid>>(StateMachineKey.CorrelationFunc);
		}
	}
}
