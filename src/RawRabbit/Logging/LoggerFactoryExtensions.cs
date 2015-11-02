namespace RawRabbit.Logging
{
	public static class LoggerFactoryExtensions
	{
		public static ILogger CreateLogger<TType>(this ILoggerFactory factory)
		{
			return factory.CreateLogger(typeof (TType).Name);
		}
	}
}
