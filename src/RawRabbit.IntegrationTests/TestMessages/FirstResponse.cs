using System;
using RawRabbit.Core.Message;

namespace RawRabbit.IntegrationTests.TestMessages
{
	public class FirstResponse : MessageBase
	{
		public Guid Infered { get; set; }
	}
}
