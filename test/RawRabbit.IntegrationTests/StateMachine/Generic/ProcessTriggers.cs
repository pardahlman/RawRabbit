using System;
using System.Collections.Generic;
using RawRabbit.Operations.StateMachine.Middleware;
using RawRabbit.Operations.StateMachine.Trigger;

namespace RawRabbit.IntegrationTests.StateMachine.Generic
{
	public class ProcessTriggers : TriggerConfiguration<GenericProcess>
	{
		public override List<TriggerPipeOptions> ConfigureTriggers(TriggerConfigurer<GenericProcess> trigger)
		{
			trigger
				.FromMessage<CreateTask>(
					process => Guid.NewGuid(),
					(task, msg) => task.CreateAsync(msg.Name, msg.DeadLine))
				.FromMessage<StartTask>(
					start => start.TaskId,
					(task, msg) => task.StartAsync(msg.Assignee))
				.FromMessage<PauseTask>(
					pause => pause.TaskId,
					(task, pause) => task.PauseAsync(pause.Reason))
				.FromMessage<ResumeTask>(
					pause => pause.TaskId,
					(task, msg) => task.ResumeAsync())
				.FromMessage<CompleteTask>(
					complete => complete.TaskId,
					(task, complete) => task.CompleteAsync())
				.FromMessage<AbortTask>(
					abort => abort.TaskId,
					(task, abort) => task.Abort(abort.Reason)
				);

			return trigger.TriggerPipeOptions;
		}
	}
}