using System;

namespace RawRabbit.Logging
{
    public class VoidLogger : ILogger
    {
        public void LogDebug(string format, params object[] args)
        {
        }

        public void LogInformation(string format, params object[] args)
        {
        }

        public void LogWarning(string format, params object[] args)
        {
        }

        public void LogError(string message, Exception exception)
        {
        }
    }

    public class VoidLoggerFactory : ILoggerFactory
    {
        private readonly VoidLogger _logger;

        public VoidLoggerFactory()
        {
            _logger = new VoidLogger();
        }
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }
    }
}
