using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
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
			publisher.PublishAsync<SimpleMessage>();
			Task.WaitAll(basicTcs.Task, simpleTcs.Task);

			/* Assert */
			Assert.True(true, "Successfully recieved messages.");
		}

		[Fact]
		public async Task Should_Throw_Publish_Confirm_Exception_If_Server_Doesnt_Respond_Within_Time_Limit()
		{
			/* Setup */
			var publisher = BusClientFactory.CreateDefault(
				new RawRabbitConfiguration
				{
					PublishConfirmTimeout = TimeSpan.FromTicks(1)
				}
			);

			/* Test */
			/* Assert */
			await Assert.ThrowsAsync<PublishConfirmException>(() => publisher.PublishAsync<BasicMessage>());
		}

		[Fact]
		public void Should_Be_Able_To_Confirm_Multiple_Messages()
		{
			/* Setup */
			const int numberOfCalls = 100;
			var confirmTasks = new Task[numberOfCalls];
			var publisher = BusClientFactory.CreateDefault(
				new RawRabbitConfiguration
				{
					PublishConfirmTimeout = TimeSpan.FromMilliseconds(500)
				}
			);

			for (int i = 0; i < numberOfCalls; i++)
			{
				var confirmTask = publisher.PublishAsync<BasicMessage>();
				confirmTasks[i] = confirmTask;
			}
			Task.WaitAll(confirmTasks);
			Task.Delay(500).Wait();

			Assert.True(true, "Successfully confirmed all messages.");
		}
	}
}
