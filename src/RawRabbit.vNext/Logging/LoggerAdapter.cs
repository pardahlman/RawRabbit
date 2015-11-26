using System;
using Microsoft.Extensions.Logging;
using ILogger = RawRabbit.Logging.ILogger;

namespace RawRabbit.vNext.Logging
{
	public class LoggerAdapter : ILogger
	{
		private readonly Microsoft.Extensions.Logging.ILogger _vNextLogger;

		public LoggerAdapter(Microsoft.Extensions.Logging.ILogger vNextLogger)
		{
			_vNextLogger = vNextLogger;
		}

		public void LogDebug(string format, params object[] args)
		{
			_vNextLogger.LogDebug(format,args);
		}

		public void LogInformation(string format, params object[] args)
		{
			_vNextLogger.LogInformation(format, args);
		}

		public void LogWarning(string format, params object[] args)
		{
			_vNextLogger.LogWarning(format, args);
		}

		public void LogError(string message, Exception exception)
		{
			_vNextLogger.LogError(message, exception);
		}
	}
}
