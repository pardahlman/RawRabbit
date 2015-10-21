using RabbitMQ.Client;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Conventions
{
	public interface IExchangeConventions
	{
		ExchangeConfiguration CreateDefaultConfiguration<T>() where T : MessageBase;
		ExchangeConfiguration CreateDefaultRpcExchange<TRequest, TResponse>();
	}

	public class ExchangeConventions : IExchangeConventions
	{
		public ExchangeConfiguration CreateDefaultConfiguration<T>() where T : MessageBase
		{
			return new ExchangeConfiguration
			{
				ExchangeName = typeof (T).Namespace.ToLower(),
				ExchangeType = RabbitMQ.Client.ExchangeType.Direct,
				Durable = true,
				AutoDelete = false
			};
		}

		public ExchangeConfiguration CreateDefaultRpcExchange<TRequest, TResponse>()
		{
			return new ExchangeConfiguration
			{
				ExchangeName = "default_rpc_exchange",
				ExchangeType = RabbitMQ.Client.ExchangeType.Direct,
				Durable = true,
				AutoDelete = false
			};
		}
	}
}
