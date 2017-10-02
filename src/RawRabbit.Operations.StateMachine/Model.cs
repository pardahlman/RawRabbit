using System;

namespace RawRabbit.Operations.StateMachine
{
	public abstract class Model<TState> : Model
	{
		public TState State { get; set; }
	}

	public abstract class Model
	{
		public Guid Id { get; set; }
	}
}