using System;

namespace RawRabbit.Context
{
	public class AdvancedMessageContext : MessageContext, IAdvancedMessageContext
	{
		public Action Nack { get; set; }
		public Action<TimeSpan> RetryLater { get; set; }
	}
}
