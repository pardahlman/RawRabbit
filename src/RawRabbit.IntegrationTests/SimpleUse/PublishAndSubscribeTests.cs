using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
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
			
			subscriber.SubscribeAsync<BasicMessage>((msg, info) =>
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

		[Fact]
		public async void Should_Be_Able_To_Perform_Multiple_Pub_Subs()
		{
			/* Setup */
			var subscriber = BusClientFactory.CreateDefault();
			var publisher = BusClientFactory.CreateDefault();
			const int numberOfCalls = 100;
			var recived = 0;
			var recievedTcs = new TaskCompletionSource<bool>();
			subscriber.SubscribeAsync<BasicMessage>((message, context) =>
			{
				Interlocked.Increment(ref recived);
				if (numberOfCalls == recived)
				{
					recievedTcs.SetResult(true);
				}
				return Task.FromResult(true);
			});

			/* Test */
			var sw = Stopwatch.StartNew();
			for (int i = 0; i < numberOfCalls; i++)
			{
				publisher.PublishAsync<BasicMessage>();
			}
			await recievedTcs.Task;
			sw.Stop();

			/* Assert */
			Assert.True(true, $"Completed {numberOfCalls} in {sw.ElapsedMilliseconds} ms.");
		}
	}
}
