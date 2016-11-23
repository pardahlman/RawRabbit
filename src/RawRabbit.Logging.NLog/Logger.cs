using System;

namespace RawRabbit.Logging.NLog
{
    public class Logger : ILogger
    {
        private readonly global::NLog.ILogger _nlog;

        public Logger(global::NLog.ILogger nlog)
        {
            _nlog = nlog;
        }

        public void LogDebug(string format, params object[] args)
        {
            _nlog.Debug(format, args);
        }

        public void LogInformation(string format, params object[] args)
        {
            _nlog.Info(format, args);
        }

        public void LogWarning(string format, params object[] args)
        {
            _nlog.Warn(format, args);
        }

        public void LogError(string message, Exception exception)
        {
            _nlog.Error(exception,message);
        }
    }
}
