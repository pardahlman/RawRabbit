namespace RawRabbit.Logging.Log4Net
{
    public class LoggerFactory : ILoggerFactory {

        public LogLevel MinimumLevel { get; set; }

        public ILogger CreateLogger(string categoryName)
        {
            var log4net = global::log4net.LogManager.GetLogger(categoryName);
            return new Logger(log4net);
        }

        public void Dispose() { }
    }
}