using System.Threading.Tasks;
using Stateless;

namespace RawRabbit.Operations.StateMachine
{
	public abstract class StateMachineBase
	{
		public abstract Task TriggerAsync(object trigger);
		public abstract Task TriggerAsync<TPayload>(object trigger, TPayload payload);
		public abstract Model GetDto();
	}

	public abstract class StateMachineBase<TState, TTrigger, TModel> : StateMachineBase where TModel : Model<TState>
	{
		protected readonly TModel Model;
		protected StateMachine<TState, TTrigger> StateMachine;

		protected StateMachineBase(TModel model = null)
		{
			Model = model ?? Initialize();
			StateMachine = new StateMachine<TState, TTrigger>(() => Model.State, s => Model.State = s);
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
		
		public override Model GetDto()
		{
			return Model;
		}
	}
}