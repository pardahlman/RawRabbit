using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Operations.Saga.Repository
{
	public interface IGlobalLock
	{
		Task ExecuteAsync(Guid sagaId, Func<Task> handler, CancellationToken ct = default(CancellationToken));
	}

	public class GlobalLock : IGlobalLock
	{
		private readonly Func<Guid, Func<Task>, CancellationToken, Task> _exclusiveExecute;

		public GlobalLock(Func<Guid, Func<Task>, CancellationToken, Task> exclusiveExecute = null)
		{
			if (exclusiveExecute == null)
			{
				var processLock = new ProcessGlobalLock();
				exclusiveExecute = (id, handler, ct) => processLock.ExecuteAsync(id, handler, ct);
			}
			_exclusiveExecute = exclusiveExecute;
		}

		public Task ExecuteAsync(Guid sagaId, Func<Task> handler, CancellationToken ct = new CancellationToken())
		{
			return _exclusiveExecute(sagaId, handler, ct);
		}
	}

	public class ProcessGlobalLock : IGlobalLock
	{
		private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _semaphores;

		public ProcessGlobalLock()
		{
			_semaphores = new ConcurrentDictionary<Guid, SemaphoreSlim>();
		}
		
		public Task ExecuteAsync(Guid sagaId, Func<Task> handler, CancellationToken ct = default(CancellationToken))
		{
			var semaphore = _semaphores.GetOrAdd(sagaId, guid => new SemaphoreSlim(1, 1));
			return semaphore
				.WaitAsync(ct)
				.ContinueWith(t => handler().ContinueWith(done => semaphore.Release(), ct), ct)
				.Unwrap();
		}
	}
}
