using System;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Operations.StateMachine.Trigger;
using RawRabbit.Pipe;

namespace RawRabbit.IntegrationTests.StateMachine.Generic
{
	public class ProcessTriggers : TriggerConfiguration
	{
		public override void ConfigureTriggers(TriggerConfigurer trigger)
		{
			trigger
				.FromMessage<GenericProcess, CreateTask>(
					process => Guid.NewGuid(),
					(task, msg) => task.CreateAsync(msg.Name, msg.DeadLine))
				.FromMessage<GenericProcess, StartTask>(
					start => start.TaskId,
					(task, msg) => task.StartAsync(msg.Assignee))
				.FromMessage<GenericProcess, PauseTask>(
					pause => pause.TaskId,
					(task, pause) => task.PauseAsync(pause.Reason))
				.FromMessage<GenericProcess, ResumeTask>(
					pause => pause.TaskId,
					(task, msg) => task.ResumeAsync())
				.FromMessage<GenericProcess, CompleteTask>(
					complete => complete.TaskId,
					(task, complete) => task.CompleteAsync())
				.FromMessage<GenericProcess, AbortTask>(
					abort => abort.TaskId,
					(task, abort) => task.Abort(abort.Reason)
				);
		}
	}

	public static class TriggerExtensions
	{
		public static Action<IPipeContext> FromMsg<TStateMachine, TMessage, TContext>(this TriggerConfigurer configurer, Func<TMessage, TContext, Guid> correlationFunc, Func<TStateMachine, TMessage, TContext> invokeFunc) where TStateMachine : StateMachineBase
		{
			return null;
		}
	}
}