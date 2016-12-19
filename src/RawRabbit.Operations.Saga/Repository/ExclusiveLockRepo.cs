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
		private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Guid>> _exitDictionary;

		public ExclusiveLockRepo()
		{
			_lockDictionary = new ConcurrentDictionary<Guid, object>();
			_exitDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Guid>>();
		}
		public Task AcquireAsync(Guid sagaId)
		{
			var sagaLock = _lockDictionary.GetOrAdd(sagaId, valueFactory: guid => new object());
			var enterTsc = new TaskCompletionSource<object>();
			Task.Run(() =>
			{
				Monitor.Enter(sagaLock);
				enterTsc.TrySetResult(sagaLock);
				var exitTsc = _exitDictionary.GetOrAdd(sagaId, guid => new TaskCompletionSource<Guid>());
				exitTsc.Task.Wait();
				Monitor.Exit(sagaLock);
			});
			return enterTsc.Task;
		}

		public Task ReleaseAsync(Guid sagaId)
		{
			TaskCompletionSource<Guid> sagaLock;
			if (_exitDictionary.TryGetValue(sagaId, out sagaLock))
			{
				sagaLock.TrySetResult(sagaId);
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
