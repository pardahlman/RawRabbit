using System;

namespace RawRabbit.IntegrationTests.TestMessages
{
	public class BasicResponse : MessageBase
	{
		public string Prop { get; set; }
		public Guid Payload { get; set; }
	}
}
