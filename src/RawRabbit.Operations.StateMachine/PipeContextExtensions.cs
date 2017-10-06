using System;
using RawRabbit.Operations.StateMachine.Context;
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

		public static Action<IPipeContext> GetContextAction(this IPipeContext context)
		{
			return context.Get<Action<IPipeContext>>(StateMachineKey.ContextAction);
		}

		public static Action<IPipeBuilder> GetPipeBuilderAction(this IPipeContext context)
		{
			return context.Get<Action<IPipeBuilder>>(StateMachineKey.PipeBuilderAction);
		}

		public static Func<object[], Guid> GetIdCorrelationFunc(this IPipeContext context)
		{
			return context.Get<Func<object[], Guid>>(StateMachineKey.CorrelationFunc);
		}

		public static object[] GetLazyCorrelationArgs(this IPipeContext context)
		{
			var func = context.Get<Func<IPipeContext, Func<IPipeContext, object[]>>>(StateMachineKey.LazyCorrelationFuncArgs);
			return func?.Invoke(context)?.Invoke(context);
		}

		public static object[] GetLazyHandlerArgs(this IPipeContext context)
		{
			var func =  context.Get<Func<IPipeContext, Func<IPipeContext, object[]>>>(StateMachineKey.LazyHandlerArgsFunc);
			return func?.Invoke(context)?.Invoke(context);
		}

		public static IStateMachineContext UseLazyCorrelationArgs(this IStateMachineContext context, Func<IPipeContext, object[]> argsFunc)
		{
			Func<IPipeContext, Func<IPipeContext, object[]>> lazyFunc = pipeContext => argsFunc;
			context.Properties.TryAdd(StateMachineKey.LazyCorrelationFuncArgs, lazyFunc);
			return context;
		}

		public static IStateMachineContext UseLazyHandlerArgs(this IStateMachineContext context, Func<IPipeContext, object[]> argsFunc)
		{
			Func<IPipeContext, Func<IPipeContext, object[]>> lazyFunc = pipeContext => argsFunc;
			context.Properties.TryAdd(StateMachineKey.LazyHandlerArgsFunc, lazyFunc);
			return context;
		}
	}
}
