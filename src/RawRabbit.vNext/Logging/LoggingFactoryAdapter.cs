using RawRabbit.Logging;

namespace RawRabbit.vNext.Logging
{
	public class LoggingFactoryAdapter : ILoggerFactory
	{
		private readonly Microsoft.Extensions.Logging.ILoggerFactory _vNextFactory;

		public LoggingFactoryAdapter(Microsoft.Extensions.Logging.ILoggerFactory vNextFactory)
		{
			_vNextFactory = vNextFactory;
		}

		public void Dispose()
		{
			_vNextFactory.Dispose();
		}

		public ILogger CreateLogger(string categoryName)
		{
			var vNextLogger =  _vNextFactory.CreateLogger(categoryName);
			return new LoggerAdapter(vNextLogger);
		}
	}
}
