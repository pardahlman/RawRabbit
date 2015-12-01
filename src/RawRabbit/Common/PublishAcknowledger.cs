using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Exceptions;
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
		private readonly TimeSpan _publishTimeout;
		private readonly ILogger _logger = LogManager.GetLogger<PublishAcknowledger>();
		private ConcurrentDictionary<ulong, TaskCompletionSource<ulong>> _deliveredAckDictionary;
		private ulong _currentDeliveryTag;

		public PublishAcknowledger(TimeSpan publishTimeout)
		{
			_publishTimeout = publishTimeout;
		}

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
					tcs.TrySetResult(args.DeliveryTag);
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
			Timer publishTimer = null;
			publishTimer = new Timer(state =>
			{
				publishTimer?.Dispose();
				TaskCompletionSource<ulong> deliveryTcs;
				if (!_deliveredAckDictionary.TryRemove(_currentDeliveryTag, out deliveryTcs))
				{
					_logger.LogWarning($"Unable to find task completion source for publish ack {_currentDeliveryTag}.");
				}
				else
				{
					deliveryTcs.TrySetException(new PublishConfirmException($"The broker did not send a publish acknowledgement for message {_currentDeliveryTag} within {_publishTimeout.ToString("g")}."));
				}
			}, null, _publishTimeout, new TimeSpan(-1));
			return tcs.Task;
		}
	}
}
