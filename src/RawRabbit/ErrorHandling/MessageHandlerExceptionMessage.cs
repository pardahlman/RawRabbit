using System;
using RawRabbit.Exceptions;

namespace RawRabbit.ErrorHandling
{
	public class HandlerExceptionMessage
	{
		public string Host { get; set; }
		public DateTime Time { get; set; }
		public object Message { get; set; }
		public ExceptionInformation Exception { get; set; }
	}
}
