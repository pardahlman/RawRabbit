using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class PublishAcknowledgeMiddleware : Middleware
	{
		private readonly IExclusiveLock _exclusive;
		private TimeSpan _publishTimeOut;
		private readonly ILogger _logger = LogManager.GetLogger<PublishAcknowledgeMiddleware>();

		public PublishAcknowledgeMiddleware(IExclusiveLock exclusive, RawRabbitConfiguration config)
		{
			_exclusive = exclusive;
			_publishTimeOut = config.PublishConfirmTimeout;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var channel = context.GetTransientChannel();
			if (channel.NextPublishSeqNo == 0UL)
			{
				_logger.LogInformation($"Setting 'Publish Acknowledge' for channel '{channel.ChannelNumber}'");
				_exclusive.Execute(channel, c => c.ConfirmSelect(), token);
			}

			var thisSequence = channel.NextPublishSeqNo;
			var ackTcs = new TaskCompletionSource<ulong>();
			Timer ackTimer = null;
			ackTimer = new Timer(state =>
			{
				ackTcs.TrySetException(
					new PublishConfirmException(
						$"The broker did not send a publish acknowledgement for message {thisSequence} on channel {channel.ChannelNumber} within {_publishTimeOut:g}."));
				ackTimer?.Dispose();
			}, channel, _publishTimeOut, new TimeSpan(-1));

			var ackedTsc = new TaskCompletionSource<ulong>();
			EventHandler<BasicAckEventArgs> channelBasicAck = null;
			channelBasicAck = (sender, args) =>
			{
				_logger.LogInformation($"Basic Ack recieved for '{args.DeliveryTag}' on channel '{channel.ChannelNumber}'");

				if (args.DeliveryTag < thisSequence)
				{
					return;
				}
				if (args.DeliveryTag != thisSequence && !args.Multiple)
				{
					return;
				}

				_logger.LogDebug($"Recieve Confirm for '{thisSequence}' on channel '{channel.ChannelNumber}'.");
				ackedTsc.TrySetResult(args.DeliveryTag);
				channel.BasicAcks -= channelBasicAck;
				ackTcs.TrySetResult(thisSequence);
			};
			channel.BasicAcks += channelBasicAck;
			context.Properties.Add(PipeKey.PublishAcknowledger, ackTcs.Task);
			return Next
				.InvokeAsync(context, token)
				.ContinueWith(t => ackTcs.Task, token)
				.Unwrap();
		}
	}
}
