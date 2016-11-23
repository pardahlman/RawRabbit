using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Logging;

namespace RawRabbit.vNext.Logging
{
    public class LoggingFactory : ILoggerFactory
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _vNextFactory;

        public static ILoggerFactory ApplicationLogger(IServiceProvider provider)
        {
            return new LoggingFactory(provider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>());
        }

        public LoggingFactory(Microsoft.Extensions.Logging.ILoggerFactory vNextFactory)
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
