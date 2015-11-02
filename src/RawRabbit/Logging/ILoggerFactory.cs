using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
}
