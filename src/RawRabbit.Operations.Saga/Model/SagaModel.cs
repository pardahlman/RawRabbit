using System;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class SagaModel<TState> : SagaModel
	{
		public TState State { get; set; }
	}

	public abstract class SagaModel
	{
		public Guid Id { get; set; }
	}
}