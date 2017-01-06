using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.StateMachine
{
	public static class StateMachineExtension
	{
		public static Task RegisterStateMachineAsync<TStateMachine, TTriggerConfiguration>(
			this IBusClient busClient,
			CancellationToken ct = default(CancellationToken))
				where TStateMachine : StateMachineBase
				where TTriggerConfiguration : TriggerConfiguration, new()
		{
			return busClient.InvokeAsync(
				builder => builder
					.Use<RepeatMiddleware>(new RepeatOptions
					{
						EnumerableFunc = context => context.GetTriggerPipeOptions(),
						RepeatContextFactory = (context, factory, enumerated) =>
						{
							var options = enumerated as TriggerPipeOptions;
							var childContext = factory.CreateContext(context.Properties.ToArray());
							childContext.Properties.TryAdd(StateMachineKey.PipeBuilderAction, options?.PipeActionFunc(childContext));
							childContext.Properties.TryAdd(StateMachineKey.ContextAction, options?.ContextActionFunc(childContext));
							return childContext;
						},
						RepeatePipe = pipe => pipe.Use<TriggerPipeMiddleware>()
					}),
				context =>
				{
					context.Properties.Add(StateMachineKey.Type, typeof(TStateMachine));
					context.Properties.Add(StateMachineKey.TriggerPipeOptions, new TTriggerConfiguration().GetTriggerPipeOptions());
				}, ct);
		}

		public static Task TriggerStateMachineAsync<TStateMachine>(
			this IBusClient busClient,
			Func<TStateMachine, Task> triggerFunc,
			Guid modelId = default(Guid),
			CancellationToken ct = default(CancellationToken)) where TStateMachine : StateMachineBase
		{
			Func<object[], Task> genericHandler = objects => triggerFunc((TStateMachine) objects[0]);

			return busClient
				.InvokeAsync(
					builder => builder
						.Use<RetrieveStateMachineMiddleware>()
						.Use<HandlerInvokationMiddleware>(new HandlerInvokationOptions
						{
							HandlerArgsFunc = context => new object[] {context.GetStateMachine()},
							MessageHandlerFunc = context => context.GetMessageHandler()
						})
						.Use<PersistModelMiddleware>()
						,
					context =>
					{
						context.Properties.Add(StateMachineKey.Type, typeof(TStateMachine));
						context.Properties.Add(PipeKey.MessageHandler, genericHandler);
						if (modelId != Guid.Empty)
						{
							context.Properties.Add(StateMachineKey.ModelId, modelId);
						}
					}, ct);
		}
	}
}
