using RawRabbit.Core.Message;

namespace RawRabbit.IntegrationTests.TestMessages
{
	public class SimpleMessage : MessageBase
	{
		public bool IsSimple { get; set; }
	}
}
