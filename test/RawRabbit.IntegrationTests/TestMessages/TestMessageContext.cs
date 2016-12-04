using System;
using RawRabbit.Context;

namespace RawRabbit.IntegrationTests.TestMessages
{
	public class TestMessageContext : IMessageContext
	{
		public string Prop { get; set; }
		public Guid GlobalRequestId { get; set; }
	}
}
