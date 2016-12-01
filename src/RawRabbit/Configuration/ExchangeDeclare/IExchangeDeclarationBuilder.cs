namespace RawRabbit.Configuration.Exchange
{
	public interface IExchangeDeclarationBuilder
	{
		IExchangeDeclarationBuilder WithName(string exchangeName);
		IExchangeDeclarationBuilder WithType(ExchangeType exchangeType);
		IExchangeDeclarationBuilder WithDurability(bool durable = true);
		IExchangeDeclarationBuilder WithAutoDelete(bool autoDelete= true);
		IExchangeDeclarationBuilder WithArgument(string name, string value);
		IExchangeDeclarationBuilder AssumeInitialized(bool asumption = true);
	}
}
