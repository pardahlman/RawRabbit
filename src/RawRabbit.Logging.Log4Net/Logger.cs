using System;
using log4net;

namespace RawRabbit.Logging.Log4Net
{
	public class Logger : ILogger
	{
		private readonly ILog _log4Net;

		public Logger(ILog log4Net)
		{
			_log4Net = log4Net;
		}

		public void LogDebug(string format, params object[] args)
		{
			_log4Net.DebugFormat(format, args);
		}

		public void LogInformation(string format, params object[] args)
		{
			_log4Net.InfoFormat(format, args);
		}

		public void LogWarning(string format, params object[] args)
		{
			_log4Net.WarnFormat(format,args);
		}

		public void LogError(string message, Exception exception)
		{
			_log4Net.Error(message, exception);
		}
	}
}
