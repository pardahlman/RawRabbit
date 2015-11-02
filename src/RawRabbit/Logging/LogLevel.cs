namespace RawRabbit.Logging
{
	/// <summary>
	/// A temporary copy of https://github.com/aspnet/Logging/blob/dev/src/Microsoft.Extensions.Logging.Abstractions/LogLevel.cs
	/// </summary>
	public enum LogLevel
	{
		/// <summary>
		/// Logs that contain the most detailed messages. These messages may contain sensitive application data.
		/// These messages are disabled by default and should never be enabled in a production environment.
		/// </summary>
		Debug = 1,

		/// <summary>
		/// Logs that are used for interactive investigation during development.  These logs should primarily contain
		/// information useful for debugging and have no long-term value.
		/// </summary>
		Verbose = 2,

		/// <summary>
		/// Logs that track the general flow of the application. These logs should have long-term value.
		/// </summary>
		Information = 3,

		/// <summary>
		/// Logs that highlight an abnormal or unexpected event in the application flow, but do not otherwise cause the
		/// application execution to stop.
		/// </summary>
		Warning = 4,

		/// <summary>
		/// Logs that highlight when the current flow of execution is stopped due to a failure. These should indicate a
		/// failure in the current activity, not an application-wide failure.
		/// </summary>
		Error = 5,

		/// <summary>
		/// Logs that describe an unrecoverable application or system crash, or a catastrophic failure that requires
		/// immediate attention.
		/// </summary>
		Critical = 6,
	}
}
