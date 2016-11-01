using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class PublishAcknowledgeMiddleware : Middleware
	{
		private TimeSpan _publishTimeOut;
		private readonly ILogger _logger = LogManager.GetLogger<PublishAcknowledgeMiddleware>();

		public PublishAcknowledgeMiddleware(RawRabbitConfiguration config)
		{
			_publishTimeOut = config.PublishConfirmTimeout;
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			var channel = context.GetChannel();
			if (channel.NextPublishSeqNo == 0UL)
			{
				_logger.LogInformation($"Setting 'Publish Acknowledge' for channel '{channel.ChannelNumber}'");
				channel.ConfirmSelect();
			}

			var thisSequence = channel.NextPublishSeqNo;
			var ackTcs = new TaskCompletionSource<ulong>();
			Timer ackTimer = null;
			ackTimer = new Timer(state =>
			{
				ackTcs.TrySetException(
					new PublishConfirmException(
						$"The broker did not send a publish acknowledgement for message {thisSequence} on channel {channel.ChannelNumber} within {_publishTimeOut.ToString("g")}."));
				ackTimer?.Dispose();
			}, channel, _publishTimeOut, new TimeSpan(-1));

			TaskCompletionSource<ulong> ackedTsc = new TaskCompletionSource<ulong>();
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
			return Next.InvokeAsync(context).ContinueWith(t => ackTcs.Task).Unwrap();
		}
	}
}
