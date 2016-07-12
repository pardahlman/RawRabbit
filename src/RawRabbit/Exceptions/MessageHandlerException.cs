using System;
using System.Runtime.Serialization;

namespace RawRabbit.Exceptions
{
	/// <summary>
	/// Exception is thrown if the message handler in a IResponder throws
	/// a unhandled exception.
	/// </summary>
	public class MessageHandlerException : Exception
	{
		public MessageHandlerException()
		{ }

		public MessageHandlerException(string message) :base(message)
		{ }

		public MessageHandlerException(string message, Exception inner)
			: base(message, inner)
		{ }
	}
}
