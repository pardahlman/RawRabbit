using System.Collections.Concurrent;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Logging;

namespace RawRabbit.Common
{
	public interface IPublishAcknowledger
	{
		void SetActiveChannel(IModel channel);
		Task GetAckTask();
	}

	public class NoAckAcknowledger : IPublishAcknowledger
	{
		public static Task Completed = Task.FromResult(0ul);
		
		public void SetActiveChannel(IModel channel)
		{ }

		public Task GetAckTask()
		{
			return Completed;
		}
	}

	public class PublishAcknowledger : IPublishAcknowledger
	{
		private readonly ILogger _logger = LogManager.GetLogger<PublishAcknowledger>();
		private ConcurrentDictionary<ulong, TaskCompletionSource<ulong>> _deliveredAckDictionary;
		private ulong _currentDeliveryTag;

		public void SetActiveChannel(IModel channel)
		{
			_deliveredAckDictionary = new ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>();
			_currentDeliveryTag = 0;
			channel.BasicAcks += (sender, args) =>
			{
				TaskCompletionSource<ulong> tcs;
				if (_deliveredAckDictionary.TryRemove(args.DeliveryTag, out tcs))
				{
					_logger.LogDebug($"Message with delivery tag {args.DeliveryTag} has been acknowledged by broker.");
					tcs.SetResult(args.DeliveryTag);
				}
				else
				{
					_logger.LogWarning($"Delivery tag {args.DeliveryTag} not found in publish acknowledger.");
				}
			};
			channel.ConfirmSelect();
		}

		public Task GetAckTask()
		{
			var tcs = new TaskCompletionSource<ulong>();
			_currentDeliveryTag++;
			if (!_deliveredAckDictionary.TryAdd(_currentDeliveryTag, tcs))
			{
				_logger.LogWarning($"Unable to add delivery tag {_currentDeliveryTag} to ack list.");
			}
			return tcs.Task;
		}
	}
}
