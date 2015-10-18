using System.Threading.Tasks;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Common
{
	public abstract class OperatorBase
	{
		private readonly IChannelFactory _channelFactory;

		protected OperatorBase(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
		}
		
		protected Task DeclareExchangeAsync(ExchangeConfiguration config)
		{
			if (config.IsDefaultExchange())
			{
				return Task.FromResult(true);
			}
			return Task.Factory.StartNew(() =>
				_channelFactory
					.GetChannel()
					.ExchangeDeclare(
						exchange: config.ExchangeName,
						type: config.ExchangeType
					)
				);
		}

		protected Task DeclareQueueAsync(QueueConfiguration config)
		{
			return Task.Factory.StartNew(() =>
				_channelFactory
					.GetChannel()
					.QueueDeclare(
						queue: config.QueueName,
						durable: config.Durable,
						exclusive: config.Exclusive,
						autoDelete: config.AutoDelete,
						arguments: config.Arguments
					)
				);
		}
	}
}
