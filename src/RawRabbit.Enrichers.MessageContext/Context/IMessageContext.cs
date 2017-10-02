using System;

namespace RawRabbit.Enrichers.MessageContext.Context
{
	public interface IMessageContext
	{
		Guid GlobalRequestId { get; set; }
	}
}