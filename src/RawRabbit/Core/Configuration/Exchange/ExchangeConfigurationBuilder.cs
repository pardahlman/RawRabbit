namespace RawRabbit.Core.Configuration.Exchange
{
	public class ExchangeConfigurationBuilder : IExchangeConfigurationBuilder
	{
		public ExchangeConfiguration Configuration { get; }

		public ExchangeConfigurationBuilder(ExchangeConfiguration initialExchange = null)
		{
			Configuration = initialExchange ?? ExchangeConfiguration.Default;
		}

		public IExchangeConfigurationBuilder WithName(string exchangeName)
		{
			Configuration.ExchangeName = exchangeName;
			return this;
		}

		public IExchangeConfigurationBuilder WithType(string exchangeType)
		{
			Configuration.ExchangeType = exchangeType;
			return this;
		}

		public IExchangeConfigurationBuilder AsDurable(bool durable = true)
		{
			Configuration.Durable = durable;
			return this;
		}

		public IExchangeConfigurationBuilder WithAutoDelete(bool autoDelete = true)
		{
			Configuration.AutoDelete = autoDelete;
			return this;
		}

		public IExchangeConfigurationBuilder WithArgument(string name, string value)
		{
			Configuration.Arguments.Add(name, value);
			return this;
		}
	}
}
