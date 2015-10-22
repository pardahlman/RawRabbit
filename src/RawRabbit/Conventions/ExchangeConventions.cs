using RawRabbit.Configuration.Exchange;

namespace RawRabbit.Conventions
{
	public interface IExchangeConventions
	{
		ExchangeConfiguration CreateDefaultConfiguration<T>();
		ExchangeConfiguration CreateDefaultRpcExchange<TRequest, TResponse>();
	}

	public class ExchangeConventions : IExchangeConventions
	{
		public ExchangeConfiguration CreateDefaultConfiguration<T>()
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
