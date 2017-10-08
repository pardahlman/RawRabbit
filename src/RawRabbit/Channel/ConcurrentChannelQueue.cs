using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Channel
{
	public class ConcurrentChannelQueue
	{
		private readonly ConcurrentQueue<TaskCompletionSource<IModel>> _queue;

		public EventHandler Queued;

		public ConcurrentChannelQueue()
		{
			_queue = new ConcurrentQueue<TaskCompletionSource<IModel>>();
		}

		public TaskCompletionSource<IModel> Enqueue()
		{
			var modelTsc = new TaskCompletionSource<IModel>();
			var raiseEvent = _queue.IsEmpty;
			_queue.Enqueue(modelTsc);
			if (raiseEvent)
			{
				Queued?.Invoke(this, EventArgs.Empty);
			}

			return modelTsc;
		}

		public bool TryDequeue(out TaskCompletionSource<IModel> channel)
		{
			return _queue.TryDequeue(out channel);
		}

		public bool IsEmpty => _queue.IsEmpty;

		public int Count => _queue.Count;
	}
}
