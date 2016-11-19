using System;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class SagaDto<TState> : SagaDto
	{
		public TState State { get; set; }
	}

	public abstract class SagaDto
	{
		public Guid Id { get; set; }
	}
}