using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Request
{
	public class RequestConfigurationBuilder : IRequestConfigurationBuilder
	{
		private QueueConfigurationBuilder _queue;
		private ExchangeConfigurationBuilder _exchange;
		public RequestConfiguration Configuration { get; }

		public RequestConfigurationBuilder(QueueConfiguration defaultQueue = null, ExchangeConfiguration defaultExchange = null)
		{
			_queue = new QueueConfigurationBuilder(defaultQueue);
			_exchange = new ExchangeConfigurationBuilder(defaultExchange);
		}
	}
}