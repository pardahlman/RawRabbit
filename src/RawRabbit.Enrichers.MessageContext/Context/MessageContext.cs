using System;

namespace RawRabbit.Enrichers.MessageContext.Context
{
	public class MessageContext : IMessageContext
	{
		public Guid GlobalRequestId { get; set; }
	}
}
