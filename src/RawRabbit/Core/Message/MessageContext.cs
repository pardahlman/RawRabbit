using System;

namespace RawRabbit.Core.Message
{
	public class MessageContext
	{
		public Guid GlobalRequestId { get; set; }
		public string SessionId { get; set; }
	}
}
