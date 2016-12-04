using System.Threading.Tasks;
using Stateless;

namespace RawRabbit.Operations.Saga.Model
{
	public abstract class Saga
	{
		public abstract Task TriggerAsync(object trigger);
		public abstract Task TriggerAsync<TPayload>(object trigger, TPayload payload);
		public abstract SagaModel GetDto();
	}

	public abstract class  Saga<TState, TTrigger> : Saga<TState, TTrigger, SagaModel<TState>> { }

	public abstract class Saga<TState, TTrigger, TModel> : Saga where TModel : SagaModel<TState>
	{
		protected readonly TModel SagaDto;
		protected StateMachine<TState, TTrigger> StateMachine;

		protected Saga(TModel model = null)
		{
			SagaDto = model ?? Initialize();
			StateMachine = new StateMachine<TState, TTrigger>(() => SagaDto.State, s => SagaDto.State = s);
			ConfigureState(StateMachine);
		}

		protected abstract void ConfigureState(StateMachine<TState, TTrigger> machine);

		public abstract TModel Initialize();

		public override Task TriggerAsync(object trigger)
		{
			return StateMachine.FireAsync((TTrigger) trigger);
		}

		public override Task TriggerAsync<TPayload>(object trigger, TPayload payload)
		{
			var paramTrigger = new StateMachine<TState, TTrigger>.TriggerWithParameters<TPayload>((TTrigger)trigger);
			return StateMachine.FireAsync(paramTrigger, payload);
		}
		
		public override SagaModel GetDto()
		{
			return SagaDto;
		}
	}
}