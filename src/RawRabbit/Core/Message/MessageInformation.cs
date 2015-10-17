using System;

namespace RawRabbit.Core.Message
{
	public class MessageInformation
	{
		public Guid GlobalRequestId { get; set; }
		public string SessionId { get; set; }
	}
}
