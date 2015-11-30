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
		public async Task Should_Be_Able_To_Subscribe_Without_Any_Additional_Config()
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
		public async Task Should_Be_Able_To_Perform_Multiple_Pub_Subs()
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

		[Fact]
		public void Should_Be_Able_To_Perform_Subscribe_For_Multiple_Types()
		{
			/* Setup */
			var subscriber = BusClientFactory.CreateDefault();
			var publisher = BusClientFactory.CreateDefault();

			var basicTcs = new TaskCompletionSource<BasicMessage>();
			var simpleTcs = new TaskCompletionSource<SimpleMessage>();
			subscriber.SubscribeAsync<BasicMessage>((message, context) =>
			{
				basicTcs.SetResult(message);
				return Task.FromResult(true);
			});
			subscriber.SubscribeAsync<SimpleMessage>((message, context) =>
			{
				simpleTcs.SetResult(message);
				return Task.FromResult(true);
			});

			/* Test */
			publisher.PublishAsync<BasicMessage>();
			publisher.PublishAsync<SimpleMessage >();
			Task.WaitAll(basicTcs.Task, simpleTcs.Task);

			/* Assert */
			Assert.True(true, "Successfully recieved messages.");
		}
	}
}
