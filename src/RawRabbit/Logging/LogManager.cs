
namespace RawRabbit.Logging
{
	public class LogManager
	{
		private static ILoggerFactory _current;
		public static ILoggerFactory CurrentFactory
		{
			get { return _current ?? (_current = new LoggerFactory()); }
			set { _current = value; }
		}

		public static ILogger GetLogger<TType>()
		{
			return CurrentFactory.CreateLogger<TType>();
		}
	}
}
