using System;

namespace RawRabbit.Context
{
	public class AdvancedMessageContext : IAdvancedMessageContext
	{
		public Guid GlobalRequestId { get; set; }
		public Action Nack { get; set; }
	}
}
