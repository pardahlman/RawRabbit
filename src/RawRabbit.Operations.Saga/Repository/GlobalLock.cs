using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Operations.Saga.Repository
{
	public interface IGlobalLock
	{
		Task AcquireAsync(Guid sagaId, CancellationToken token = default(CancellationToken));
		Task ReleaseAsync(Guid sagaId, CancellationToken token = default(CancellationToken));
		Task ExecuteAsync(Guid sagaId, Func<Task> handler, CancellationToken token = default(CancellationToken));
	}

	public class GlobalLock : IGlobalLock
	{
		private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _semaphores;
		private readonly ConcurrentDictionary<Guid, TaskCompletionSource<Guid>> _exitDictionary;

		public GlobalLock()
		{
			_semaphores = new ConcurrentDictionary<Guid, SemaphoreSlim>();
			_exitDictionary = new ConcurrentDictionary<Guid, TaskCompletionSource<Guid>>();
		}

		public Task AcquireAsync(Guid sagaId, CancellationToken token = default(CancellationToken))
		{
			var enterTsc = new TaskCompletionSource<object>();
			var exitTsc = _exitDictionary.GetOrAdd(sagaId, guid => new TaskCompletionSource<Guid>());
			
			var semanphore = _semaphores.GetOrAdd(sagaId, guid => new SemaphoreSlim(1, 1));
			token.Register(() =>
			{
				if (semanphore.CurrentCount == 0)
					semanphore.Release();
			});
			semanphore
				.WaitAsync(token)
				.ContinueWith(t =>
				{
					enterTsc.TrySetResult(null);
					exitTsc.Task.ContinueWith(done =>
					{
						semanphore.Release();
					}, token);
				}, token);
			return enterTsc.Task;
		}

		public Task ReleaseAsync(Guid sagaId, CancellationToken token = default(CancellationToken))
		{
			TaskCompletionSource<Guid> sagaLock;
			if (_exitDictionary.TryGetValue(sagaId, out sagaLock))
			{
				sagaLock.TrySetResult(sagaId);
			}
			return Task.FromResult(0);
		}

		public Task ExecuteAsync(Guid sagaId, Func<Task> handler, CancellationToken token = default(CancellationToken))
		{
			var semaphore = _semaphores.GetOrAdd(sagaId, guid => new SemaphoreSlim(1, 1));
			return semaphore
				.WaitAsync(token)
				.ContinueWith(t => handler().ContinueWith(done => semaphore.Release(), token), token)
				.Unwrap();
		}
	}
}
