using System;
using Serilog.Core;

namespace RawRabbit.Logging.Serilog
{
	public class LoggerFactory : ILoggerFactory
	{
		private readonly Func<string, global::Serilog.ILogger> _createFunc;
		public LogLevel MinimumLevel { get; set; }

		public LoggerFactory(Func<string, global::Serilog.ILogger> createFunc = null)
		{
			_createFunc = createFunc ?? (category => global::Serilog.Log.ForContext(Constants.SourceContextPropertyName, category));
		}

		public ILogger CreateLogger(string categoryName)
		{
			var serilog = _createFunc(categoryName);
			return new Logger(serilog);
		}

		public void Dispose()
		{
		}
	}
}
