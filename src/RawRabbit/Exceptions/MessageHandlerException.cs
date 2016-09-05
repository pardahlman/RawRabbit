using System;

namespace RawRabbit.Exceptions
{
	/// <summary>
	/// Exception is thrown if the message handler in a IResponder throws
	/// a unhandled exception.
	/// </summary>
	public class MessageHandlerException : Exception
	{
		public string InnerExceptionType { get; set; }
		public string InnerStackTrace { get; set; }
		public string InnerMessage { get; set; }

		public MessageHandlerException()
		{ }

		public MessageHandlerException(string message) :base(message)
		{ }

		public MessageHandlerException(string message, Exception inner)
			: base(message, inner)
		{ }

	}
}
