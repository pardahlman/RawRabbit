using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Exceptions;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class PublishAcknowledgeOptions
	{
		public Func<IPipeContext, TimeSpan> TimeOutFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> EnabledFunc { get; set; }
	}

	public class PublishAcknowledgeMiddleware : Middleware
	{
		private readonly IExclusiveLock _exclusive;
		private readonly ILogger _logger = LogManager.GetLogger<PublishAcknowledgeMiddleware>();
		protected Func<IPipeContext, TimeSpan> TimeOutFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Func<IPipeContext, bool> EnabledFunc;

		public PublishAcknowledgeMiddleware(IExclusiveLock exclusive, PublishAcknowledgeOptions options = null)
		{
			_exclusive = exclusive;
			TimeOutFunc = options?.TimeOutFunc ?? (context => context.GetClientConfiguration().PublishConfirmTimeout);
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			EnabledFunc = options?.EnabledFunc ?? (context => context.GetClientConfiguration().PublishConfirmTimeout != TimeSpan.MaxValue);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var enabled = GetEnabled(context);
			if (!enabled)
			{
				_logger.LogDebug("Publish Acknowledgement is not disabled.");
				return Next.InvokeAsync(context, token);
			}

			var channel = GetChannel(context);
			if (channel.NextPublishSeqNo == 0UL)
			{
				EnableAcknowledgement(channel, token);
			}

			var thisSequence = channel.NextPublishSeqNo;
			var ackTcs = new TaskCompletionSource<ulong>();
			SetupTimeout(context, thisSequence, ackTcs);

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
				channel.BasicAcks -= channelBasicAck;
				ackTcs.TrySetResult(thisSequence);
			};
			channel.BasicAcks += channelBasicAck;
			context.Properties.TryAdd(PipeKey.PublishAcknowledger, ackTcs.Task);
			return Next
				.InvokeAsync(context, token)
				.ContinueWith(t => ackTcs.Task, token)
				.Unwrap();
		}

		protected virtual TimeSpan GetAcknowledgeTimeOut(IPipeContext context)
		{
			return TimeOutFunc(context);
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}

		protected virtual bool GetEnabled(IPipeContext context)
		{
			return EnabledFunc(context);
		}

		protected virtual void EnableAcknowledgement(IModel channel, CancellationToken token)
		{
			_logger.LogInformation($"Setting 'Publish Acknowledge' for channel '{channel.ChannelNumber}'");
			_exclusive.Execute(channel, c => c.ConfirmSelect(), token);
		}

		protected virtual void SetupTimeout(IPipeContext context, ulong sequence, TaskCompletionSource<ulong> ackTcs)
		{
			var timeout = GetAcknowledgeTimeOut(context);
			Timer ackTimer = null;
			_logger.LogInformation($"Setting up publish acknowledgement for {sequence} with timeout {timeout:g}");
			ackTimer = new Timer(state =>
			{
				ackTcs.TrySetException(
					new PublishConfirmException(
						$"The broker did not send a publish acknowledgement for message {sequence} within {timeout:g}."));
				ackTimer?.Dispose();
			}, null, timeout, new TimeSpan(-1));
		}
	}
}
