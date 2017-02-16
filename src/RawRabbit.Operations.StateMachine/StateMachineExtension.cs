using System;
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
		public static async Task RegisterStateMachineAsync<TStateMachine, TTriggerConfiguration>(
			this IBusClient busClient,
			CancellationToken ct = default(CancellationToken))
				where TStateMachine : StateMachineBase
				where TTriggerConfiguration : TriggerConfiguration, new()
		{
			var contextActions = new TTriggerConfiguration().GetTriggerContextActions();
			foreach (var contextAction in contextActions)
			{
				await busClient.RegisterTrigger(context =>
				{
					context.Properties.Add(StateMachineKey.Type, typeof(TStateMachine));
					contextAction?.Invoke(context);
				}, ct);
			}
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
