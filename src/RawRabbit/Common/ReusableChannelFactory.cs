using System;
using System.Threading;
using RabbitMQ.Client;
using Timer = System.Threading.Timer;

namespace RawRabbit.Common
{
	public class ReusableChannelFactory : IChannelFactory
	{
		private readonly IConnectionBroker _broker;
		private Timer _channelTimer;
		private ThreadLocal<IModel> _threadModel; 

		public ReusableChannelFactory(IConnectionBroker broker)
		{
			_broker = broker;
		}

		public void Dispose()
		{
			_broker?.Dispose();
		}

		public IModel GetChannel()
		{
			if (_threadModel == null)
			{
				_threadModel = new ThreadLocal<IModel>(() => _broker.GetConnection().CreateModel());
				_channelTimer = new Timer(state =>
				{
					_threadModel?.Dispose();
					_threadModel = null;
				}, null, TimeSpan.FromMilliseconds(200), new TimeSpan(-1));
			}
			else
			{
				_channelTimer.Change(TimeSpan.FromMilliseconds(100), new TimeSpan(-1));
			}

			if (_threadModel.IsValueCreated && _threadModel.Value.IsOpen)
			{
				return _threadModel.Value;
			}
			_threadModel?.Value?.Dispose();
			try
			{
				_threadModel.Value = _broker.GetConnection().CreateModel();
			}
			catch (ObjectDisposedException)
			{
				return GetChannel();
			}
			return _threadModel.Value;
		}

		public IModel CreateChannel()
		{
			return _broker.GetConnection().CreateModel();
		}
	}
}
