using System;

namespace RawRabbit.Exceptions
{
	public class ChannelAvailabilityException : Exception
	{
		public ChannelAvailabilityException(string message) : base(message)
		{ }
	}
}
