using RabbitMQ.Client;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Conventions
{
	public interface IExchangeConventions
	{
		ExchangeConfiguration CreateDefaultConfiguration<T>() where T : MessageBase;
	}

	public class ExchangeConventions : IExchangeConventions
	{
		public ExchangeConfiguration CreateDefaultConfiguration<T>() where T : MessageBase
		{
			return new ExchangeConfiguration
			{
				ExchangeName = typeof (T).Namespace.ToLower(),
				ExchangeType = ExchangeType.Direct,
				Durable = true,
				AutoDelete = false
			};
		}
	}
}
