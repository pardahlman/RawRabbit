using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace RawRabbit.Operations.StateMachine.Core
{
	public interface IModelRepository
	{
		Task<Model> GetAsync(Guid id);
		Task AddOrUpdateAsync(Model model);
	}

	public class ModelRepository : IModelRepository
	{
		private readonly Func<Guid, Task<Model>> _get;
		private readonly Func<Model, Task> _addOrUpdate;

		public ModelRepository(
			Func<Guid, Task<Model>> get = null,
			Func<Model, Task> addOrUpdate = null)
		{
			_get = get;
			_addOrUpdate = addOrUpdate;
			if (_get == null && _addOrUpdate == null)
			{
				var fallback = new ConcurrentDictionary<Guid, Model>();
				_get = id =>
				{
					Model model;
					return fallback.TryGetValue(id, out model)
						? Task.FromResult(model)
						: Task.FromResult<Model>(null);
				};
				_addOrUpdate = model =>
				{
					fallback.AddOrUpdate(model.Id, guid => model, (id, m) => model);
					return Task.FromResult(0);
				};
			}
		}

		public Task<Model> GetAsync(Guid id)
		{
			return _get(id);
		}

		public Task AddOrUpdateAsync(Model model)
		{
			return _addOrUpdate(model);
		}
	}
}