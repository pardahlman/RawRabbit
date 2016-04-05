using System;

namespace RawRabbit.Logging.NLog
{
	public class LoggerFactory : ILoggerFactory
	{
		public LogLevel MinimumLevel { get; set; }

		private readonly Func<string, global::NLog.Logger> _createFunc;

		public LoggerFactory(Func<string, global::NLog.Logger> createFunc = null)
		{
			_createFunc = createFunc ?? (categoryName => global::NLog.LogManager.GetLogger(categoryName));
		}

		public ILogger CreateLogger(string categoryName)
		{
			var nlog = _createFunc(categoryName);
			return new Logger(nlog);
		}

		public void Dispose() { }
	}
}
