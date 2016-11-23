using System;
using System.Collections.Concurrent;

namespace RawRabbit.Logging
{
    public interface ILoggerFactory : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ILogger"/> instance.
        /// </summary>
        /// <param name="categoryName">The category name for messages produced by the logger.</param>
        /// <returns>The <see cref="ILogger"/>.</returns>
        ILogger CreateLogger(string categoryName);
    }

    public class LoggerFactory : ILoggerFactory
    {
        private readonly Func<string, ILogger> _createFn;
        private readonly ConcurrentDictionary<string, ILogger> _categoryToLogger;

        public LoggerFactory() : this(s => new ConsoleLogger(LogLevel.Debug, s))
        {
            
        }
        public LoggerFactory(Func<string, ILogger> createFn)
        {
            _createFn = createFn;
            _categoryToLogger = new ConcurrentDictionary<string, ILogger>();
        }

        public void Dispose() { }

        public ILogger CreateLogger(string categoryName)
        {
            ILogger logger;
            if (!_categoryToLogger.TryGetValue(categoryName, out logger))
            {
                logger = _categoryToLogger.GetOrAdd(categoryName, _createFn);
            }
            return logger;
        }
    }

    public static class LoggerFactoryExtensions
    {
        public static ILogger CreateLogger<TType>(this ILoggerFactory factory)
        {
            return factory.CreateLogger(typeof(TType).Name);
        }
    }
}
