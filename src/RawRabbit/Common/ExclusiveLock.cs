using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;

namespace RawRabbit.Common
{
	public interface IExclusiveLock
	{
		Task<object> AquireAsync(object obj, CancellationToken token = default(CancellationToken));
		Task ReleaseAsync(object obj);
		void Execute<T>(T obj, Action<T> action, CancellationToken token = default(CancellationToken));
		Task ExecuteAsync<T>(T obj, Func<T, Task> func, CancellationToken token = default(CancellationToken));
	}

	public class ExclusiveLock : IExclusiveLock, IDisposable
	{
		private readonly ConcurrentDictionary<object, SemaphoreSlim> _semaphoreDictionary;
		private readonly ConcurrentDictionary<object, object> _lockDictionary;
		private readonly ILog _logger = LogProvider.For<ExclusiveLock>();

		public ExclusiveLock()
		{
			_semaphoreDictionary = new ConcurrentDictionary<object, SemaphoreSlim>();
			_lockDictionary = new ConcurrentDictionary<object, object>();
		}

		public Task<object> AquireAsync(object obj, CancellationToken token = default(CancellationToken))
		{
			var theLock = _lockDictionary.GetOrAdd(obj, o => new object());
			var semaphore = _semaphoreDictionary.GetOrAdd(theLock, o => new SemaphoreSlim(1,1));
			return semaphore
				.WaitAsync(token)
				.ContinueWith(t => theLock, token);
		}

		public Task ReleaseAsync(object obj)
		{
			var semaphore = _semaphoreDictionary.GetOrAdd(obj, o => new SemaphoreSlim(1, 1));
			semaphore.Release();
			return Task.FromResult(0);
		}

		public void Execute<T>(T obj, Action<T> action, CancellationToken token = default(CancellationToken))
		{
			var theLock = _lockDictionary.GetOrAdd(obj, o => new object());
			var semaphore = _semaphoreDictionary.GetOrAdd(theLock, o => new SemaphoreSlim(1, 1));
			semaphore.Wait(token);
			try
			{
				action(obj);
			}
			catch (Exception e)
			{
				_logger.Error("Exception when performing exclusive execute", e);
			}
			finally
			{
				semaphore.Release();
			}
		}

		public async Task ExecuteAsync<T>(T obj, Func<T, Task> func, CancellationToken token = default(CancellationToken))
		{
			var theLock = _lockDictionary.GetOrAdd(obj, o => new object());
			var semaphore = _semaphoreDictionary.GetOrAdd(theLock, o => new SemaphoreSlim(1, 1));
			await semaphore.WaitAsync(token);
			try
			{
				await func(obj);
			}
			catch (Exception e)
			{
				_logger.ErrorException("Exception when performing exclusive executeasync", e);
			}
			finally
			{
				semaphore.Release();
			}
		}

		public void Dispose()
		{
			foreach (var slim in _semaphoreDictionary.Values)
			{
				slim.Dispose();
			}
		}
	}
}
