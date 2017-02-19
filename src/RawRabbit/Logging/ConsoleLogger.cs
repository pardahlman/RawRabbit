using System;
using System.Threading;
using Newtonsoft.Json;

namespace RawRabbit.Logging
{
	public class ConsoleLogger : ILogger
	{
		private readonly LogLevel _minLevel;
		private readonly string _category;

		public ConsoleLogger(LogLevel minLevel, string category)
		{
			_minLevel = minLevel;
			_category = category;
		}

		public void LogDebug(string format, params object[] args)
		{
			if (LogLevel.Debug < _minLevel)
			{
				return;
			}
			var message = FormatEntry(format, args);
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] [${Thread.CurrentThread.ManagedThreadId}] [DEBUG] [{_category}]: {message}");
		}

		public void LogInformation(string format, params object[] args)
		{
			if (LogLevel.Information < _minLevel)
			{
				return;
			}
			var message = FormatEntry(format, args);
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] [${Thread.CurrentThread.ManagedThreadId}] [INFO] [{_category}]: {message}");
		}

		public void LogWarning(string format, params object[] args)
		{
			if (LogLevel.Warning < _minLevel)
			{
				return;
			}
			var message = FormatEntry(format, args);
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] [${Thread.CurrentThread.ManagedThreadId}] [WARN] [{_category}]: {message}");
		}

		public void LogError(string message, Exception exception)
		{
			if (LogLevel.Error < _minLevel)
			{
				return;
			}
			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] [${Thread.CurrentThread.ManagedThreadId}] [ERROR] [{_category}]: {message}, Exception: {Environment.NewLine} {exception}");
		}

		private static string FormatEntry(string format, object[] args)
		{
			args = args ?? new object[0];
			for (var i = 0; i < args.Length; i++)
			{
				if (args[i] is string)
				{
					continue;
				}
				try
				{
					args[i] = JsonConvert.SerializeObject(args[i]);
				}
				catch (Exception)
				{
					args[i] = "N/A";
				}
			}
			return string.Format(format, args);
		}
	}
}
