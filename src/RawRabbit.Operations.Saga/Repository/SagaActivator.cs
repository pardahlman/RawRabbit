using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.DependecyInjection;
using RawRabbit.Operations.Saga.Model;

namespace RawRabbit.Operations.Saga.Repository
{
	public interface ISagaActivator
	{
		Task<Model.Saga> ActivateAsync(Guid id, Type sagaType);
		Task PersistAsync(Model.Saga saga);
	}

	public interface IModelRepository
	{
		Task<SagaModel> GetAsync(Guid id);
		Task AddOrUpdateAsync(SagaModel saga);
	}

	public class ModelRepository : IModelRepository
	{
		private readonly Func<Guid, Task<SagaModel>> _get;
		private readonly Func<SagaModel, Task> _addOrUpdate;

		public ModelRepository(
			Func<Guid, Task<SagaModel>> get = null,
			Func<SagaModel, Task> addOrUpdate = null)
		{
			_get = get;
			_addOrUpdate = addOrUpdate;
			if (_get == null && _addOrUpdate == null)
			{
				var fallback = new ConcurrentDictionary<Guid, SagaModel>();
				_get = id =>
				{
					SagaModel model;
					return fallback.TryGetValue(id, out model)
						? Task.FromResult(model)
						: Task.FromResult<SagaModel>(null);
				};
				_addOrUpdate = model =>
				{
					fallback.AddOrUpdate(model.Id, guid => model, (id, m) => model);
					return Task.FromResult(0);
				};
			}
		}

		public Task<SagaModel> GetAsync(Guid id)
		{
			return _get(id);
		}

		public Task AddOrUpdateAsync(SagaModel saga)
		{
			return _addOrUpdate(saga);
		}
	}

	public class SagaActivator : ISagaActivator
	{
		private readonly IModelRepository _modelRepo;
		private readonly IDependecyResolver _resolver;

		public SagaActivator(IModelRepository modelRepo, IDependecyResolver resolver)
		{
			_modelRepo = modelRepo;
			_resolver = resolver;
		}

		public async Task<Model.Saga> ActivateAsync(Guid id, Type sagaType)
		{
			var model = await _modelRepo.GetAsync(id);
			if (model != null)
			{
				var saga = _resolver.GetService(sagaType, model) as Model.Saga;
				return saga;
			}
			var newSaga = _resolver.GetService(sagaType) as Model.Saga;
			var newSagaModel = newSaga.GetDto();
			newSagaModel.Id = id;
			await _modelRepo.AddOrUpdateAsync(newSagaModel);
			return newSaga;
		}
		
		public async Task PersistAsync(Model.Saga saga)
		{
			await _modelRepo.AddOrUpdateAsync(saga.GetDto());
		}
	}
}
