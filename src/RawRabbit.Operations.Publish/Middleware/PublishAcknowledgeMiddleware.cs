using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Exceptions;
using RawRabbit.Logging;
using RawRabbit.Operations.Publish.Context;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class PublishAcknowledgeOptions
	{
		public Func<IPipeContext, TimeSpan> TimeOutFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> EnabledFunc { get; set; }
	}

	public class PublishAcknowledgeMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IExclusiveLock _exclusive;
		private readonly ILog _logger = LogProvider.For<PublishAcknowledgeMiddleware>();
		protected Func<IPipeContext, TimeSpan> TimeOutFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Func<IPipeContext, bool> EnabledFunc;

		protected static Dictionary<IModel, ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>> ConfirmsDictionary =
			new Dictionary<IModel, ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>>();
		protected static ConcurrentDictionary<IModel, object> ChannelLocks = new ConcurrentDictionary<IModel, object>();
		protected static Dictionary<IModel, ulong> ChannelSequences = new Dictionary<IModel, ulong>();

		public PublishAcknowledgeMiddleware(IExclusiveLock exclusive, PublishAcknowledgeOptions options = null)
		{
			_exclusive = exclusive;
			TimeOutFunc = options?.TimeOutFunc ?? (context => context.GetPublishAcknowledgeTimeout());
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			EnabledFunc = options?.EnabledFunc ?? (context => context.GetPublishAcknowledgeTimeout() != TimeSpan.MaxValue);
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var enabled = GetEnabled(context);
			if (!enabled)
			{
				_logger.Debug("Publish Acknowledgement is disabled.");
				await Next.InvokeAsync(context, token);
				return;
			}
			var channel = GetChannel(context);

			if (!PublishAcknowledgeEnabled(channel))
			{
				EnableAcknowledgement(channel, token);
			}

			var channelLock = ChannelLocks.GetOrAdd(channel, c => new object());
			var ackTcs = new TaskCompletionSource<ulong>();

			await _exclusive.ExecuteAsync(channelLock, o =>
			{
				var sequence = channel.NextPublishSeqNo;
				SetupTimeout(context, sequence, ackTcs);
				if (!GetChannelDictionary(channel).TryAdd(sequence, ackTcs))
				{
					_logger.Info("Unable to add ack '{publishSequence}' on channel {channelNumber}", sequence, channel.ChannelNumber);
				}
				_logger.Info("Sequence {sequence} added to dictionary", sequence);

				return Next.InvokeAsync(context, token);
			}, token);
			await ackTcs.Task;
		}

		protected virtual TimeSpan GetAcknowledgeTimeOut(IPipeContext context)
		{
			return TimeOutFunc(context);
		}

		protected virtual bool PublishAcknowledgeEnabled(IModel channel)
		{
			return channel.NextPublishSeqNo != 0UL;
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}

		protected virtual bool GetEnabled(IPipeContext context)
		{
			return EnabledFunc(context);
		}

		protected virtual ConcurrentDictionary<ulong, TaskCompletionSource<ulong>> GetChannelDictionary(IModel channel)
		{
			if (!ConfirmsDictionary.ContainsKey(channel))
			{
				ConfirmsDictionary.Add(channel, new ConcurrentDictionary<ulong, TaskCompletionSource<ulong>>());
			}
			return ConfirmsDictionary[channel];
		}

		protected virtual void EnableAcknowledgement(IModel channel, CancellationToken token)
		{
			_logger.Info("Setting 'Publish Acknowledge' for channel '{channelNumber}'", channel.ChannelNumber);
			_exclusive.Execute(channel, c =>
			{
				if (PublishAcknowledgeEnabled(c))
				{
					return;
				}
				c.ConfirmSelect();
				var dictionary = GetChannelDictionary(c);
				c.BasicAcks += (sender, args) =>
				{
					Task.Run(() =>
					{
						if (args.Multiple)
						{
							foreach (var deliveryTag in dictionary.Keys.Where(k => k <= args.DeliveryTag).ToList())
							{
								if (!dictionary.TryRemove(deliveryTag, out var tcs))
								{
									continue;
								}
								if (!tcs.TrySetResult(deliveryTag))
								{
									continue;
								}
							}
						}
						else
						{
							_logger.Info("Recieived ack for {deliveryTag}", args.DeliveryTag);
							if (!dictionary.TryRemove(args.DeliveryTag, out var tcs))
							{
								_logger.Warn("Unable to find ack tcs for {deliveryTag}", args.DeliveryTag);
							}
							tcs?.TrySetResult(args.DeliveryTag);
						}
					}, token);
				};
			}, token);
		}

		protected virtual void SetupTimeout(IPipeContext context, ulong sequence, TaskCompletionSource<ulong> ackTcs)
		{
			var timeout = GetAcknowledgeTimeOut(context);
			Timer ackTimer = null;
			_logger.Info("Setting up publish acknowledgement for {publishSequence} with timeout {timeout:g}", sequence, timeout);
			ackTimer = new Timer(state =>
			{
				ackTcs.TrySetException(new PublishConfirmException($"The broker did not send a publish acknowledgement for message {sequence} within {timeout:g}."));
				ackTimer?.Dispose();
			}, null, timeout, new TimeSpan(-1));
		}
	}

	public static class PublishAcknowledgePipeGetExtensions
	{
		public static TimeSpan GetPublishAcknowledgeTimeout(this IPipeContext context)
		{
			var fallback = context.GetClientConfiguration().PublishConfirmTimeout;
			return context.Get(PublishKey.PublishAcknowledgeTimeout, fallback);
		}
	}
}

namespace RawRabbit
{
	public static class PublishAcknowledgePipeUseExtensions
	{
		public static IPublishContext UsePublishAcknowledge(this IPublishContext context, TimeSpan timeout)
		{
			context.Properties.TryAdd(Operations.Publish.PublishKey.PublishAcknowledgeTimeout, timeout);
			return context;
		}

		public static IPublishContext UsePublishAcknowledge(this IPublishContext context, bool use = true)
		{
			return !use
				? context.UsePublishAcknowledge(TimeSpan.MaxValue)
				: context;
		}
	}
}

