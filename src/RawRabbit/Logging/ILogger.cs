using System;

namespace RawRabbit.Logging
{
    /*
        This interface is an abstract of signatures for Microsoft.Extensions.Logging's ILogger and its extensionmethods.
    */
    public interface ILogger
    {
        void LogDebug(string format, params object[] args);
        void LogInformation(string format, params object[] args);
        void LogWarning(string format, params object[] args);
        void LogError(string message, Exception exception);
    }
}
