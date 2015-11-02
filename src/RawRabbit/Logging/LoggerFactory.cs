using System;

namespace RawRabbit.Logging
{
	public class LoggerFactory : ILoggerFactory
	{
		private readonly Func<LogLevel, string, ILogger> _createFn;

		public LoggerFactory() : this((level, s) => new ConsoleLogger(level, s))
		{
			
		}
		public LoggerFactory(Func<LogLevel, string, ILogger> createFn)
		{
			_createFn = createFn;
		}

		public void Dispose()
		{
		}

		public LogLevel MinimumLevel { get; set; }

		public ILogger CreateLogger(string categoryName)
		{
			return _createFn(MinimumLevel, categoryName);
		}
	}
}
