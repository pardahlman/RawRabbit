using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Operations.Saga.Repository
{
	public interface IExclusiveLockRepo
	{
		Task AcquireAsync(Guid sagaId);
		Task ReleaseAsync(Guid sagaId);
		Task ExecuteAsync(Guid sagaId, Func<Task> handler);
	}

	public class ExclusiveLockRepo : IExclusiveLockRepo
	{
		private readonly ConcurrentDictionary<Guid, object> _lockDictionary;

		public ExclusiveLockRepo()
		{
			_lockDictionary = new ConcurrentDictionary<Guid, object>();
		}
		public Task AcquireAsync(Guid sagaId)
		{
			var sagaLock = _lockDictionary.GetOrAdd(sagaId, valueFactory: guid => new object());
			var tcs = new TaskCompletionSource<object>();
			Task.Run(() =>
			{
				Monitor.Enter(sagaLock);
				tcs.TrySetResult(sagaLock);
			});
			return tcs.Task;
		}

		public Task ReleaseAsync(Guid sagaId)
		{
			object sagaLock;
			if (_lockDictionary.TryGetValue(sagaId, out sagaLock))
			{
				Monitor.Exit(sagaLock);
			}
			return Task.FromResult(0);
		}

		public Task ExecuteAsync(Guid sagaId, Func<Task> handler)
		{
			return Task.Run(() =>
			{
				var sagaLock = _lockDictionary.GetOrAdd(sagaId, valueFactory: guid => new object());
				Monitor.Enter(sagaLock);
				handler().Wait();
				Monitor.Exit(sagaLock);
			});
		}
	}
}
