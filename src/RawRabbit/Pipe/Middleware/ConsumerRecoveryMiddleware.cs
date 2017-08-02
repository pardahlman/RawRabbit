using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Configuration.Consume;
using RawRabbit.Consumer;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumerRecoveryOptions
	{
		public Func<IPipeContext, bool> EnabledFunc { get; set; }
		public Func<IPipeContext, IBasicConsumer> ConsumerFunc { get; set; }
		public Func<IPipeContext, TimeSpan> RecoverTimeSpan { get; set; }
		public Func<IPipeContext, ConsumeConfiguration> ConsumeConfigFunc { get; set; }
	}

	public class ConsumerRecoveryMiddleware : Middleware
	{
		protected Func<IPipeContext, IBasicConsumer> ConsumerFunc;
		protected Func<IPipeContext, TimeSpan> RecoverTimeSpan;
		protected Func<IPipeContext, bool> EnabledFunc;
		protected Func<IPipeContext, ConsumeConfiguration> ConsumeConfigFunc;
		private readonly ILog _logger = LogProvider.For<ConsumerRecoveryMiddleware>();

		public ConsumerRecoveryMiddleware(ConsumerRecoveryOptions options = null)
		{
			ConsumerFunc = options?.ConsumerFunc ?? (context => context.GetConsumer());
			RecoverTimeSpan = options?.RecoverTimeSpan ?? (context => context.GetConsumerRecoveryTimeout());
			EnabledFunc = options?.EnabledFunc ?? (context => context.GetConsumerRecoveryEnabled());
			ConsumeConfigFunc = options?.ConsumeConfigFunc ?? (context => context.GetConsumeConfiguration());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			if (!IsEnabled(context))
			{
				_logger.Debug("Consumer Recovery Disabled.");
				await Next.InvokeAsync(context, token);
				return;
			}

			var basicConsumer = GetConsumer(context) as EventingBasicConsumer;
			if (basicConsumer == null)
			{
				await Next.InvokeAsync(context, token);
				return;
			}
			basicConsumer.Shutdown += (sender, args) =>
			{
				_logger.Info("Consumer has been shut down.\n  Reason: {shutdowCause}\n  Initiator: {shutdownInitiator}\n  Reply Text: {shutdownReplyText}", args.Cause, args.Initiator, args.ReplyText);
				if (args.Initiator == ShutdownInitiator.Application)
				{
					_logger.Info($"Initiator is Application. No further action will be taken");
					return;
				}
				var consumer = sender as IBasicConsumer;
				var config = GetConsumeConfig(context);
				config.ConsumerTag = Guid.NewGuid().ToString();
				_logger.Debug("Updating consumer tag to {consumerTag}", config.ConsumerTag);
				var channel = consumer.Model;
				if (channel.IsOpen)
				{
					_logger.Info("Channel {channelNumber} is open. Reconnecting consumer.", channel.ChannelNumber);
					WireUpConsumer(channel, consumer, config);
					return;
				}
				_logger.Info("Channel '{channelNumber}' is closed.", channel.ChannelNumber);
				if (!IsRecoverable(channel))
				{
					_logger.Info("Channel '{channelNumber}' is not recoverable. No further action will be taken.", channel.ChannelNumber);
					return;
				}

				var recoverable = channel as IRecoverable;
				EventHandler<EventArgs> consumeOnRecovery = null;
				consumeOnRecovery = (o, eventArgs) =>
				{
					_logger.Info("Recovery event recieved. Wiring up consumer on {channelNumber}.", channel.ChannelNumber);
					recoverable.Recovery -= consumeOnRecovery;
					WireUpConsumer(channel, consumer, config);
				};
				recoverable.Recovery += consumeOnRecovery;

				Timer retryTimoutTimer = null;
				retryTimoutTimer = new Timer(state =>
				{
					_logger.Info("The retry timeout has expired. No further action will be taken.");
					retryTimoutTimer?.Dispose();
					recoverable.Recovery -= consumeOnRecovery;
				}, null, GetRecoverTimeSpan(context), new TimeSpan(-1));
			};
			
			await Next.InvokeAsync(context, token);
		}

		protected virtual IBasicConsumer GetConsumer(IPipeContext context)
		{
			return ConsumerFunc(context);
		}

		protected virtual TimeSpan GetRecoverTimeSpan(IPipeContext context)
		{
			return RecoverTimeSpan(context);
		}

		protected virtual bool IsRecoverable(IModel channel)
		{
			return channel is IRecoverable;
		}

		protected virtual bool IsEnabled(IPipeContext context)
		{
			return EnabledFunc(context);
		}

		protected virtual ConsumeConfiguration GetConsumeConfig(IPipeContext context)
		{
			return ConsumeConfigFunc(context);
		}

		protected virtual void WireUpConsumer(IModel channel,IBasicConsumer consumer, ConsumeConfiguration config)
		{
			channel.BasicConsume(
				config.QueueName,
				config.NoAck,
				config.ConsumerTag,
				config.NoLocal,
				config.Exclusive,
				config.Arguments,
				consumer);
		}
	}

	public static class ConsumerRecoveryExtensions
	{
		private const string ConsumerRecoveryEnabled = "ConsumerRecoveryEnabled";
		private const string ConsumerRecoveryTimeout = "ConsumerRecoveryTimeout";

		public static IPipeContext UseConsumerRecovery(this IPipeContext context, bool enabled = true)
		{
			context.Properties.TryAdd(ConsumerRecoveryEnabled, enabled);
			return context;
		}

		public static IPipeContext UseConsumerRecovery(this IPipeContext context, TimeSpan recoverTimeOut)
		{
			context.UseConsumerRecovery();
			context.Properties.TryAdd(ConsumerRecoveryTimeout, recoverTimeOut);
			return context;
		}

		public static bool GetConsumerRecoveryEnabled(this IPipeContext context)
		{
			return context.Get(ConsumerRecoveryEnabled, true);
		}

		public static TimeSpan GetConsumerRecoveryTimeout(this IPipeContext context)
		{
			return context.Get(ConsumerRecoveryTimeout, context.GetClientConfiguration().RequestTimeout);
		}
	}
}
