using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Logging;

namespace RawRabbit.Channel
{
	public class AutoScalingChannelPool : DynamicChannelPool
	{
		private readonly IChannelFactory _factory;
		private readonly AutoScalingOptions _options;
		private Timer _timer;
		private readonly ILog _logger = LogProvider.For<AutoScalingChannelPool>();

		public AutoScalingChannelPool(IChannelFactory factory, AutoScalingOptions options)
		{
			_factory = factory;
			_options = options;
			ValidateOptions(options);
			SetupScaling();
		}

		private static void ValidateOptions(AutoScalingOptions options)
		{
			if (options.MinimunPoolSize <= 0)
			{
				throw new ArgumentException($"Minimum Pool Size needs to be a positive integer. Got: {options.MinimunPoolSize}");
			}
			if (options.MaximumPoolSize <= 0)
			{
				throw new ArgumentException($"Maximum Pool Size needs to be a positive integer. Got: {options.MinimunPoolSize}");
			}
			if (options.MinimunPoolSize > options.MaximumPoolSize)
			{
				throw new ArgumentException($"The Maximum Pool Size ({options.MaximumPoolSize}) must be larger than the Minimum Pool Size ({options.MinimunPoolSize})");
			}
		}

		public override async Task<IModel> GetAsync(CancellationToken ct = default(CancellationToken))
		{
			var activeChannels = GetActiveChannelCount();
			if (activeChannels  < _options.MinimunPoolSize)
			{
				_logger.Debug("Pool currently has {channelCount}, which is lower than the minimal pool size {minimalPoolSize}. Creating channels.", activeChannels, _options.MinimunPoolSize);
				var delta = _options.MinimunPoolSize - Pool.Count;
				for (var i = 0; i < delta; i++)
				{
					var channel = await _factory.CreateChannelAsync(ct);
					Add(channel);
				}
			}

			return await base.GetAsync(ct);
		}

		public void SetupScaling()
		{
			if (_options.RefreshInterval == TimeSpan.MaxValue || _options.RefreshInterval == TimeSpan.MinValue)
			{
				return;
			}

			_timer = new Timer(state =>
			{
				var workPerChannel = Pool.Count == 0 ? int.MaxValue : ChannelRequestQueue.Count / Pool.Count;
				var scaleUp = Pool.Count < _options.MaximumPoolSize;
				var scaleDown = _options.MinimunPoolSize < Pool.Count;

				_logger.Debug("Channel pool currently has {channelCount} channels open and a total workload of {totalWorkload}", Pool.Count, ChannelRequestQueue.Count);
				if (scaleUp && _options.DesiredAverageWorkload < workPerChannel)
				{
					_logger.Debug("The estimated workload is {averageWorkload} operations/channel, which is higher than the desired workload ({desiredAverageWorkload}). Creating channel.", workPerChannel, _options.DesiredAverageWorkload);

					var channelCancellation = new CancellationTokenSource(_options.RefreshInterval);
					_factory
						.CreateChannelAsync(channelCancellation.Token)
						.ContinueWith(tChannel =>
						{
							if (tChannel.Status == TaskStatus.RanToCompletion)
							{
								Add(tChannel.Result);
							}
						}, CancellationToken.None);
					return;
				}

				if (scaleDown && workPerChannel < _options.DesiredAverageWorkload)
				{
					_logger.Debug("The estimated workload is {averageWorkload} operations/channel, which is lower than the desired workload ({desiredAverageWorkload}). Creating channel.", workPerChannel, _options.DesiredAverageWorkload);
					var toRemove = Pool.FirstOrDefault();
					Pool.Remove(toRemove);
					Timer disposeTimer = null;
					disposeTimer = new Timer(o =>
					{
						(o as IModel)?.Dispose();
						disposeTimer?.Dispose();
					}, toRemove, _options.GracefulCloseInterval, new TimeSpan(-1));
				}
			}, null, _options.RefreshInterval, _options.RefreshInterval);
		}

		public override void Dispose()
		{
			base.Dispose();
			_timer?.Dispose();
		}
	}

	public class AutoScalingOptions
	{
		public int DesiredAverageWorkload { get; set; }
		public int MinimunPoolSize { get; set; }
		public int MaximumPoolSize { get; set; }
		public TimeSpan RefreshInterval { get; set; }
		public TimeSpan GracefulCloseInterval { get; set; }

		public static AutoScalingOptions Default => new AutoScalingOptions
		{
			MinimunPoolSize = 1,
			MaximumPoolSize = 10,
			DesiredAverageWorkload = 20000,
			RefreshInterval = TimeSpan.FromSeconds(10),
			GracefulCloseInterval = TimeSpan.FromSeconds(30)
		};
	}
}
