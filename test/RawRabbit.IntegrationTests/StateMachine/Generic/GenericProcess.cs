using System;
using System.Threading.Tasks;
using RawRabbit.Operations.StateMachine;
using Stateless;

namespace RawRabbit.IntegrationTests.StateMachine.Generic
{
	public class GenericProcess : StateMachineBase<State, Trigger, GenericProcessModel>
	{
		private readonly IBusClient _client;
		private StateMachine<State, Trigger>.TriggerWithParameters<string> _cancel;
		private StateMachine<State, Trigger>.TriggerWithParameters<string> _pause;

		public GenericProcess(IBusClient client, GenericProcessModel model = null) : base(model)
		{
			_client = client;
		}

		protected override void ConfigureState(StateMachine<State, Trigger> process)
		{
			_cancel = process.SetTriggerParameters<string>(Trigger.Cancel);
			_pause = process.SetTriggerParameters<string>(Trigger.Pausing);

			process
				.Configure(State.Created)
				.PermitIf(Trigger.Start, State.InProgress, IsAssigned)
				.Permit(Trigger.Cancel, State.Aborted);

			process
				.Configure(State.InProgress)
				.OnEntryAsync(() => SendUpdateMessage())
				.Permit(Trigger.Completion, State.Completed)
				.Permit(Trigger.Pausing, State.Paused)
				.Permit(Trigger.Cancel, State.Aborted);

			process
				.Configure(State.Paused)
				.OnEntryFromAsync(_pause, SendUpdateMessage)
				.PermitIf(Trigger.Resuming, State.InProgress, IsAssigned)
				.Permit(Trigger.Cancel, State.Aborted);

			process
				.Configure(State.Aborted)
				.OnEntryFromAsync(_cancel, SendAbortMessage);

			process
				.Configure(State.Completed)
				.OnEntryAsync(SendCompletionMessage);
		}

		private Task SendCompletionMessage()
		{
			return _client.PublishAsync(new ProcessCompeted
			{
				TaskId = Model.Id
			});
		}

		private Task SendAbortMessage(string reason)
		{
			return _client.PublishAsync(new ProcessAborted
			{
				TaskId = Model.Id,
				Reason = reason
			});
		}

		private bool IsAssigned()
		{
			return !string.IsNullOrWhiteSpace(Model.Assignee);
		}

		private Task SendUpdateMessage(string message = null)
		{
			return _client.PublishAsync(new ProcessUpdated
			{
				TaskId = Model.Id,
				State = Model.State,
				Assignee = Model.Assignee,
				Message = message
			});
		}

		public Task StartAsync(string assignee)
		{
			Model.Assignee = assignee;
			return StateMachine.FireAsync(Trigger.Start);
		}

		public Task PauseAsync(string reason)
		{
			return StateMachine.FireAsync(_pause, reason);
		}

		public Task ResumeAsync()
		{
			return StateMachine.FireAsync(Trigger.Resuming);
		}

		public void Abort(string reason)
		{
			StateMachine.Fire(_cancel, reason);
		}

		public Task CreateAsync(string process, DateTime deadline)
		{
			Model.Name = process;
			Model.Deadline = deadline;
			Model.Id = Guid.NewGuid();
			return _client.PublishAsync(new TaskCreated
			{
				Name = process,
				TaskId = Model.Id
			});
		}

		public Task CompleteAsync()
		{
			return StateMachine.FireAsync(Trigger.Completion);
		}

		public override GenericProcessModel Initialize()
		{
			return new GenericProcessModel
			{
				State = State.Created,
				Id = Guid.NewGuid()
			};
		}
	}
}
