using System.Collections.Generic;
using System.Linq;
using Stateless;

namespace RawRabbit.Operations.Saga.Repository
{
	public class TriggerParameterRepository<TState, TTrigger>
	{
		private readonly IDictionary<object, List<object>> _triggers;
		private readonly StateMachine<TState, TTrigger> _machine;

		public TriggerParameterRepository(StateMachine<TState, TTrigger> machine)
		{
			this._machine = machine;
			_triggers = new Dictionary<object, List<object>>();
		}

		public StateMachine<TState, TTrigger>.TriggerWithParameters<TPayload> Get<TPayload>(TTrigger trigger)
		{
			if (!_triggers.ContainsKey(_machine))
			{
				_triggers.Add(_machine, new List<object>());
			}
			var allreadyReged = _triggers[_machine]
					.OfType<StateMachine<TState, TTrigger>.TriggerWithParameters<TPayload>>()
					.FirstOrDefault(p => p.Trigger.Equals(trigger));
			if (allreadyReged != null)
			{
				return allreadyReged;
			}
				
			var triggerParam = _machine.SetTriggerParameters<TPayload>(trigger);
			_triggers[_machine].Add(triggerParam);

			return triggerParam;
		}
	}
}
