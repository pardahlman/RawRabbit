using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.DependecyInjection;

namespace RawRabbit.Operations.Saga.Repository
{
	public interface ISagaRepository
	{
		Task<Model.Saga> GetAsync(Guid id, Type sagaType);
		Task UpdateAsync(Model.Saga saga);
	}

	public class InMemoryRepository : ISagaRepository
	{
		private readonly IDependecyResolver _resolver;
		private readonly Dictionary<Guid, object> _dtoDictionary;

		public InMemoryRepository(IDependecyResolver resolver)
		{
			_resolver = resolver;
			_dtoDictionary = new Dictionary<Guid, object>();
		}

		public Task<Model.Saga> GetAsync(Guid id, Type sagaType)
		{
			if (_dtoDictionary.ContainsKey(id))
			{
				var dto = _dtoDictionary[id];
				var saga = _resolver.GetService(sagaType, dto) as Model.Saga;
				return Task.FromResult(saga);
			}
			var newSaga = _resolver.GetService(sagaType) as Model.Saga;
			var newSagaDto = newSaga.GetDto();
			newSagaDto.Id = id;
			_dtoDictionary.Add(newSagaDto.Id, newSagaDto);
			return Task.FromResult(newSaga);
		}
		
		public Task UpdateAsync(Model.Saga saga)
		{
			var id = saga.GetDto().Id;
			if (_dtoDictionary.ContainsKey(id))
			{
				_dtoDictionary[id] = saga.GetDto();
			}
			else
			{
				_dtoDictionary.Add(id, saga.GetDto());
			}
			return Task.FromResult(saga);
		}
	}
}
