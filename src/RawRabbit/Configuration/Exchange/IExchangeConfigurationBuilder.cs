namespace RawRabbit.Configuration.Exchange
{
    public interface IExchangeConfigurationBuilder
    {
        IExchangeConfigurationBuilder WithName(string exchangeName);
        IExchangeConfigurationBuilder WithType(ExchangeType exchangeType);
        IExchangeConfigurationBuilder WithDurability(bool durable = true);
        IExchangeConfigurationBuilder WithAutoDelete(bool autoDelete= true);
        IExchangeConfigurationBuilder WithArgument(string name, string value);
        IExchangeConfigurationBuilder AssumeInitialized(bool asumption = true);
    }
}
