namespace RawRabbit.Compatibility.Legacy.Configuration.Exchange
{
	public static class ExchangeConfigurationExtensions
	{
		public static bool IsDefaultExchange(this ExchangeConfiguration configuration)
		{
			return string.IsNullOrEmpty(configuration.ExchangeName);
		}
	}
}
