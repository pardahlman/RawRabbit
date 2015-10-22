using System;

namespace RawRabbit.IntegrationTests.TestMessages
{
	public class SecondResponse : MessageBase
	{
		public Guid Source { get; set; }
	}
}
