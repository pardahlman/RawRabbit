using RawRabbit.Common;

namespace RawRabbit.Configuration.Exchange
{
	public class ExchangeDeclarationBuilder : IExchangeDeclarationBuilder
	{
		public ExchangeDeclaration Declaration { get; }

		public ExchangeDeclarationBuilder(ExchangeDeclaration initialExchange = null)
		{
			Declaration = initialExchange ?? ExchangeDeclaration.Default;
		}

		public IExchangeDeclarationBuilder WithName(string exchangeName)
		{
			Truncator.Truncate(ref exchangeName);
			Declaration.Name = exchangeName;
			return this;
		}

		public IExchangeDeclarationBuilder WithType(ExchangeType exchangeType)
		{
			Declaration.ExchangeType = exchangeType.ToString().ToLower();
			return this;
		}

		public IExchangeDeclarationBuilder WithDurability(bool durable = true)
		{
			Declaration.Durable = durable;
			return this;
		}

		public IExchangeDeclarationBuilder WithAutoDelete(bool autoDelete = true)
		{
			Declaration.AutoDelete = autoDelete;
			return this;
		}

		public IExchangeDeclarationBuilder WithArgument(string name, string value)
		{
			Declaration.Arguments.Add(name, value);
			return this;
		}
	}
}