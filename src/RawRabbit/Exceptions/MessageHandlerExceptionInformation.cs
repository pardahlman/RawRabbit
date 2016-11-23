namespace RawRabbit.Exceptions
{
    /// <summary>
    /// Holds information about exception thrown in a remote message handler. 
    /// </summary>
    public class MessageHandlerExceptionInformation
    {
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public string StackTrace { get; set; }
        public string InnerMessage { get; set; }
    }
}
