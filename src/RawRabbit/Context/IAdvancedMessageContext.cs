using System;

namespace RawRabbit.Context
{
	public interface IAdvancedMessageContext : IMessageContext
	{
		Action Nack { get; set; }
		Action<TimeSpan> RetryLater { get; set; }
		RetryInformation RetryInfo { get; set; }
	}
}
