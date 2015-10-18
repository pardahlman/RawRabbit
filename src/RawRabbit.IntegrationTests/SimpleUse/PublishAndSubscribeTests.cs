using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Client;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class PublishAndSubscribeTests : IntegrationTestBase
	{
		public override void Dispose()
		{
			TestChannel.QueueDelete("basicmessage");
			TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages");
			base.Dispose();
		}

		[Fact]
		public async void Should_Be_Able_To_Subscribe_Without_Any_Additional_Config()
		{
			/* Setup */
			var message = new BasicMessage { Prop = "Hello, world!" };
			var recievedTcs = new TaskCompletionSource<BasicMessage>();
			
			var publisher = BusClientFactory.CreateDefault();
			var subscriber = BusClientFactory.CreateDefault();
			
			await subscriber.SubscribeAsync<BasicMessage>((msg, info) =>
			{
				recievedTcs.SetResult(msg);
				return recievedTcs.Task;
			});

			/* Test */
			publisher.PublishAsync(message);
			await recievedTcs.Task;

			/* Assert */
			Assert.Equal(recievedTcs.Task.Result.Prop, message.Prop);
		}
	}
}
