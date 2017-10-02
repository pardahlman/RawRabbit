using System;

namespace RawRabbit.IntegrationTests.StateMachine.Generic
{
	public class CreateTask
	{
		public string Name { get; set; }
		public DateTime DeadLine { get; set; }
	}

	public class TaskCreated
	{
		public Guid TaskId { get; set; }
		public string Name { get; set; }
	}

	public class StartTask
	{
		public Guid TaskId { get; set; }	
		public string Assignee { get; set; }
	}

	public class ProcessUpdated
	{
		public Guid TaskId { get; set; }
		public State State { get; set; }
		public string Message { get; set; }
		public string Assignee { get; set; }
	}

	public class AbortTask
	{
		public Guid TaskId { get; set; }
		public string Reason { get; set; }
	}

	public class ProcessAborted
	{
		public Guid TaskId { get; set; }
		public string Reason { get; set; }
	}

	public class PauseTask
	{
		public Guid TaskId { get; set; }
		public string Reason { get; set; }
	}

	public class ResumeTask
	{
		public Guid TaskId { get; set; }
		public string Message { get; set; }
	}

	public class CompleteTask
	{
		public Guid TaskId { get; set; }
	}

	public class ProcessCompeted
	{
		public Guid TaskId { get; set; }
	}
}
