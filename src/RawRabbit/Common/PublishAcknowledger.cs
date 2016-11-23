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
        Task GetAckTask(IModel result);
    }

    public class NoAckAcknowledger : IPublishAcknowledger
    {
        public static Task Completed = Task.FromResult(0ul);

        public Task GetAckTask(IModel result)
        {
            return Completed;
        }
    }

    public class PublishAcknowledger : IPublishAcknowledger
    {
        private readonly TimeSpan _publishTimeout;
        private readonly ILogger _logger = LogManager.GetLogger<PublishAcknowledger>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ulong>> _deliveredAckDictionary;
        private readonly ConcurrentDictionary<string, Timer> _ackTimers;

        public PublishAcknowledger(TimeSpan publishTimeout)
        {
            _publishTimeout = publishTimeout;
            _deliveredAckDictionary = new ConcurrentDictionary<string, TaskCompletionSource<ulong>>();
            _ackTimers = new ConcurrentDictionary<string, Timer>();
        }

        public Task GetAckTask(IModel channel)
        {
            if (channel.NextPublishSeqNo == 0UL)
            {
                _logger.LogInformation($"Setting 'Publish Acknowledge' for channel '{channel.ChannelNumber}'");
                channel.ConfirmSelect();
                channel.BasicAcks += (sender, args) =>
                {
                    var model = sender as IModel;
                    _logger.LogInformation($"Recieved ack for {args.DeliveryTag}/{model.ChannelNumber} with multiple set to '{args.Multiple}'");
                    if (args.Multiple)
                    {
                        for (var i = args.DeliveryTag; i > 0; i--)
                        {
                            CompleteConfirm(model, i, true);
                        }
                    }
                    else
                    {
                        CompleteConfirm(model, args.DeliveryTag);
                    }
                };
            }

            var key = CreatePublishKey(channel, channel.NextPublishSeqNo);
            var tcs = new TaskCompletionSource<ulong>();
            if (!_deliveredAckDictionary.TryAdd(key, tcs))
            {
                _logger.LogWarning($"Unable to add delivery tag {key} to ack list.");
            }
            _ackTimers.TryAdd(key, new Timer(state =>
            {
                _logger.LogWarning($"Ack for {key} has timed out.");
                TryDisposeTimer(key);

                TaskCompletionSource<ulong> ackTcs;
                if (!_deliveredAckDictionary.TryGetValue(key, out ackTcs))
                {
                    _logger.LogWarning($"Unable to get TaskCompletionSource for {key}");
                    return;
                }
                ackTcs.TrySetException(new PublishConfirmException($"The broker did not send a publish acknowledgement for message {key} within {_publishTimeout.ToString("g")}."));
            }, channel, _publishTimeout, new TimeSpan(-1)));
            return tcs.Task;
        }

        private void CompleteConfirm(IModel channel, ulong tag, bool multiple = false)
        {
            var key = CreatePublishKey(channel, tag);
            TryDisposeTimer(key);
            TaskCompletionSource<ulong> tcs;
            if (!_deliveredAckDictionary.TryRemove(key, out tcs))
            {
                if (!multiple)
                {
                    _logger.LogWarning($"Unable to remove task completion source for Publish Confirm on '{key}'.");
                }
            }
            else
            {
                if (tcs.TrySetResult(tag))
                {
                    _logger.LogDebug($"Successfully confirmed publish {key}");
                }
                else
                {
                    if (tcs.Task.IsFaulted)
                    {
                        _logger.LogDebug($"Unable to set result for '{key}'. Task has been faulted.");
                    }
                    else if (!multiple)
                    {
                        _logger.LogWarning($"Unable to set result for Publish Confirm on key '{key}'.");
                    }
                }
            }
        }

        private static string CreatePublishKey(IModel channel, ulong nextTag)
        {
            return $"{nextTag}/{channel.ChannelNumber}";
        }

        private void TryDisposeTimer(string key)
        {
            Timer ackTimer;
            if (!_ackTimers.TryGetValue(key, out ackTimer))
            {
                _logger.LogDebug($"Unable to get ack timer for {key}.");
            }
            else
            {
                _logger.LogDebug($"Disposed ack timer for {key}");
                ackTimer.Dispose();
            }
        }
    }
}
