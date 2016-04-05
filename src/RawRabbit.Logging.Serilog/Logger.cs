using System;

namespace RawRabbit.Logging.Serilog
{
	public class Logger : ILogger
	{
		private readonly global::Serilog.ILogger _serilog;

		public Logger(global::Serilog.ILogger serilog)
		{
			_serilog = serilog;
		}

		public void LogDebug(string format, params object[] args)
		{
			_serilog.Debug(format, args);
		}

		public void LogError(string message, Exception exception)
		{
			_serilog.Error(exception, message);
		}

		public void LogInformation(string format, params object[] args)
		{
			_serilog.Information(format, args);
		}

		public void LogWarning(string format, params object[] args)
		{
			_serilog.Warning(format, args);
		}
	}
}
