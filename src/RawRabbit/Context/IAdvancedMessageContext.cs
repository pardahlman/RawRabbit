using System;

namespace RawRabbit.Context
{
	public interface IAdvancedMessageContext : IMessageContext
	{
		Action Nack { get; set; }
	}
}
