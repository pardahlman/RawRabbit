using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class SubscribeToDifferentMessageTypes : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Be_Able_To_Recieve_Different_Types_Of_Messages()
		{
			/* Setup */
			var publisher = BusClientFactory.CreateDefault();
			var subscriber = BusClientFactory.CreateDefault();

			var basicMsg = new BasicMessage {Prop = "Hello, world!"};
			var simpleMsg = new SimpleMessage {IsSimple = true};

			var basicMsgTcs = new TaskCompletionSource<BasicMessage>();
			var simpleMsgTcs = new TaskCompletionSource<SimpleMessage>();

			subscriber.SubscribeAsync<BasicMessage>((msg, i) =>
			{
				basicMsgTcs.SetResult(msg);
				return basicMsgTcs.Task;
			});
			subscriber.SubscribeAsync<SimpleMessage>((msg, i) =>
			{
				simpleMsgTcs.SetResult(msg);
				return basicMsgTcs.Task;
			});

			/* Test */
			publisher.PublishAsync(basicMsg);
			publisher.PublishAsync(simpleMsg);
			Task.WaitAll(basicMsgTcs.Task, simpleMsgTcs.Task);

			/* Assert */
			Assert.Equal(basicMsgTcs.Task.Result.Prop, basicMsg.Prop);
			Assert.Equal(simpleMsgTcs.Task.Result.IsSimple, simpleMsg.IsSimple);
		}

		public override void Dispose()
		{
			TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages");
			TestChannel.QueueDelete("basicmessage");
			TestChannel.QueueDelete("simplemessage");
			base.Dispose();
		}
	}
}
