namespace RawRabbit.Core.Configuration.Exchange
{
	public static class ExchangeConfigurationExtensions
	{
		public static bool IsDefaultExchange(this ExchangeConfiguration configuration)
		{
			return string.IsNullOrEmpty(configuration.ExchangeName);
		}
	}
}
