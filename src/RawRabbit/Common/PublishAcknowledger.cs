using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
		private ConcurrentDictionary<ulong, Timer> _ackTimers;
		private IModel _channel;

		public PublishAcknowledger(TimeSpan publishTimeout)
		{
			_publishTimeout = publishTimeout;
		}

		public void SetActiveChannel(IModel channel)
		{
			_deliveredAckDictionary = new ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>();
			_ackTimers = new ConcurrentDictionary<ulong, Timer>();
			_channel = channel;
			_channel.BasicAcks += (sender, args) =>
			{
				_logger.LogInformation($"Recieved ack for {args.DeliveryTag} with multiple set to '{args.Multiple}'");
				if (args.Multiple)
				{
					_logger.LogInformation($"Woop woop, is multiple {args.DeliveryTag}");
					for (var i = args.DeliveryTag; i > 0; i--)
					{
						DisposeTimer(i);
					}
				}
				else
				{
					DisposeTimer(args.DeliveryTag);
				}

			};
			channel.ConfirmSelect();
		}

		private void DisposeTimer(ulong tag)
		{
			TaskCompletionSource<ulong> tcs;
			if (_deliveredAckDictionary.TryRemove(tag, out tcs))
			{
				Timer ackTimer;
				if (_ackTimers.TryRemove(tag, out ackTimer))
				{
					_logger.LogDebug($"Disposed ack timer for {tag}");
					ackTimer.Dispose();
				}
				else
				{
					_logger.LogDebug($"$Unable to find ack timer for {tag}. It has probably been ack'ed allready.");
				}
				_logger.LogDebug($"Message with delivery tag {tag} has been acknowledged by broker.");
				tcs.TrySetResult(tag);
			}
		}

		public Task GetAckTask()
		{
			var tcs = new TaskCompletionSource<ulong>();
			var nextTag = _channel.NextPublishSeqNo;
			if (!_deliveredAckDictionary.TryAdd(nextTag, tcs))
			{
				_logger.LogWarning($"Unable to add delivery tag {nextTag} to ack list.");
			}
			else
			{
				_logger.LogDebug($"Successfully added ack task for {nextTag}.");
			}
			_ackTimers.TryAdd(nextTag, new Timer(state =>
			{
				_logger.LogWarning($"Ack for {nextTag} has timed out.");
				_ackTimers[nextTag].Dispose();
				_deliveredAckDictionary[nextTag].TrySetException(
						new PublishConfirmException($"The broker did not send a publish acknowledgement for message {nextTag} within {_publishTimeout.ToString("g")}."));
			}, null, _publishTimeout, new TimeSpan(-1)));
			return tcs.Task;
		}
	}
}
