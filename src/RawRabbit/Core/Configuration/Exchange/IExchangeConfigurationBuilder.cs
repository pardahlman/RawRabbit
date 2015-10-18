namespace RawRabbit.Core.Configuration.Exchange
{
	public interface IExchangeConfigurationBuilder
	{
		IExchangeConfigurationBuilder WithName(string exchangeName);
		IExchangeConfigurationBuilder WithType(string exchangeType); //TODO: enum?
		IExchangeConfigurationBuilder AsDurable(bool durable = true);
		IExchangeConfigurationBuilder WithAutoDelete(bool autoDelete= true);
		IExchangeConfigurationBuilder WithArgument(string name, string value);
	}
}
