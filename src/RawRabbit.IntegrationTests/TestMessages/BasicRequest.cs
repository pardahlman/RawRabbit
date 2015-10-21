using RawRabbit.Core.Message;

namespace RawRabbit.IntegrationTests.TestMessages
{
	public class BasicRequest : MessageBase
	{
		public int Number { get; set; }
	}
}
