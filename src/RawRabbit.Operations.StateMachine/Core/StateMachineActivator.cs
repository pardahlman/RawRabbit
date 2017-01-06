using System;
using System.Threading.Tasks;
using RawRabbit.DependecyInjection;

namespace RawRabbit.Operations.StateMachine.Core
{
	public interface IStateMachineActivator
	{
		Task<StateMachineBase> ActivateAsync(Guid id, Type stateMachineType);
		Task PersistAsync(StateMachineBase stateMachine);
	}

	public class StateMachineActivator : IStateMachineActivator
	{
		private readonly IModelRepository _modelRepo;
		private readonly IDependecyResolver _resolver;

		public StateMachineActivator(IModelRepository modelRepo, IDependecyResolver resolver)
		{
			_modelRepo = modelRepo;
			_resolver = resolver;
		}

		public async Task<StateMachineBase> ActivateAsync(Guid id, Type stateMachineType)
		{
			var model = await _modelRepo.GetAsync(id);
			if (model != null)
			{
				var machine = _resolver.GetService(stateMachineType, model) as StateMachineBase;
				return machine;
			}
			var newMachine = _resolver.GetService(stateMachineType) as StateMachineBase;
			var newModel = newMachine.GetDto();
			newModel.Id = id;
			await _modelRepo.AddOrUpdateAsync(newModel);
			return newMachine;
		}
		
		public async Task PersistAsync(StateMachineBase stateMachine)
		{
			await _modelRepo.AddOrUpdateAsync(stateMachine.GetDto());
		}
	}
}
