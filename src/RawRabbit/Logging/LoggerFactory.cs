using System;

namespace RawRabbit.Logging
{
	public interface ILoggerFactory : IDisposable
	{
		/// <summary>
		/// The minimum level of log messages sent to loggers.
		/// </summary>
		LogLevel MinimumLevel { get; set; }

		/// <summary>
		/// Creates a new <see cref="ILogger"/> instance.
		/// </summary>
		/// <param name="categoryName">The category name for messages produced by the logger.</param>
		/// <returns>The <see cref="ILogger"/>.</returns>
		ILogger CreateLogger(string categoryName);
	}

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

	public static class LoggerFactoryExtensions
	{
		public static ILogger CreateLogger<TType>(this ILoggerFactory factory)
		{
			return factory.CreateLogger(typeof(TType).Name);
		}
	}
}
