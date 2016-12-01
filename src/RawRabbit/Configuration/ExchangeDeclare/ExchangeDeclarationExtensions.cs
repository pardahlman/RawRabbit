namespace RawRabbit.Configuration.Exchange
{
	public static class ExchangeDeclarationExtensions
	{
		public static bool IsDefaultExchange(this ExchangeDeclaration declaration)
		{
			return string.IsNullOrEmpty(declaration.ExchangeName);
		}
	}
}
