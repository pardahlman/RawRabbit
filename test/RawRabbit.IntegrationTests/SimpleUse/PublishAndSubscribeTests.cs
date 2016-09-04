using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class PublishAndSubscribeTests : IntegrationTestBase
	{

		[Fact]
		public async Task Should_Be_Able_To_Subscribe_Without_Any_Additional_Config()
		{
			/* Setup */
			using (var publisher = BusClientFactory.CreateDefault())
			using (var subscriber = BusClientFactory.CreateDefault())
			{
				var message = new BasicMessage { Prop = "Hello, world!" };
				var recievedTcs = new TaskCompletionSource<BasicMessage>();

				subscriber.SubscribeAsync<BasicMessage>((msg, info) =>
				{
					if (msg.Prop == message.Prop)
					{
						recievedTcs.SetResult(msg);
					}
					return Task.FromResult(true);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				publisher.PublishAsync(message);
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(expected: message.Prop, actual: recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Perform_Multiple_Pub_Subs()
		{
			/* Setup */
			using (var subscriber = BusClientFactory.CreateDefault())
			using (var publisher = BusClientFactory.CreateDefault())
			{
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
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

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

		[Fact]
		public void Should_Be_Able_To_Perform_Subscribe_For_Multiple_Types()
		{
			/* Setup */
			using (var subscriber = BusClientFactory.CreateDefault())
			using (var publisher = BusClientFactory.CreateDefault())
			{
				var basicTcs = new TaskCompletionSource<BasicMessage>();
				var simpleTcs = new TaskCompletionSource<SimpleMessage>();
				subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					basicTcs.SetResult(message);
					return Task.FromResult(true);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));
				subscriber.SubscribeAsync<SimpleMessage>((message, context) =>
				{
					simpleTcs.SetResult(message);
					return Task.FromResult(true);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				publisher.PublishAsync<BasicMessage>();
				publisher.PublishAsync<SimpleMessage>();
				Task.WaitAll(basicTcs.Task, simpleTcs.Task);

				/* Assert */
				Assert.True(true, "Successfully recieved messages.");
			}
		}

		[Fact]
		public async Task Should_Throw_Publish_Confirm_Exception_If_Server_Doesnt_Respond_Within_Time_Limit()
		{
			/* Setup */
			var config = RawRabbitConfiguration.Local;
			config.PublishConfirmTimeout = TimeSpan.FromTicks(1);
			using (var publisher = BusClientFactory.CreateDefault(config))
			{
				/* Test */
				/* Assert */
				try
				{
					await publisher.PublishAsync<BasicMessage>();
				}
				catch (PublishConfirmException)
				{
					Assert.True(true);
				}
			}
		}

		[Fact]
		public void Should_Be_Able_To_Confirm_Multiple_Messages()
		{
			/* Setup */
			const int numberOfCalls = 100;
			var confirmTasks = new Task[numberOfCalls];
			var config = RawRabbitConfiguration.Local;
			config.PublishConfirmTimeout = TimeSpan.FromMilliseconds(500);
			using (var publisher = BusClientFactory.CreateDefault(config))
			{
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

		[Fact]
		public void Should_Be_Able_To_Delivery_Message_To_Multiple_Subscribers_On_Same_Host()
		{
			/* Setup */
			using (var subscriber = BusClientFactory.CreateDefault())
			using (var publisher = BusClientFactory.CreateDefault())
			{
				var firstTcs = new TaskCompletionSource<bool>();
				var secondTcs = new TaskCompletionSource<bool>();
				subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					firstTcs.SetResult(true);
					return Task.FromResult(true);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));
				subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					secondTcs.SetResult(true);
					return Task.FromResult(true);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				var ackTask = publisher.PublishAsync<BasicMessage>();
				Task.WaitAll(ackTask, firstTcs.Task, secondTcs.Task);

				/* Assert */
				Assert.True(true, "Published and subscribe sucessfull.");
			}
		}

		[Fact]
		public void Should_Be_Able_To_Deliver_Messages_To_Unique_Subscribers()
		{
			/* Setup */
			using (var firstSubscriber = BusClientFactory.CreateDefault())
			using (var secondSubscriber = BusClientFactory.CreateDefault())
			using (var publisher = BusClientFactory.CreateDefault())
			{
				var firstTcs = new TaskCompletionSource<bool>();
				var secondTcs = new TaskCompletionSource<bool>();
				firstSubscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					firstTcs.SetResult(true);
					return Task.FromResult(true);
				}, cfg => cfg.WithSubscriberId("first_subscriber").WithQueue(q => q.WithAutoDelete()));
				secondSubscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					secondTcs.SetResult(true);
					return Task.FromResult(true);
				}, cfg => cfg.WithSubscriberId("second_subscriber").WithQueue(q => q.WithAutoDelete()));

				/* Test */
				var ackTask = publisher.PublishAsync<BasicMessage>();
				Task.WaitAll(ackTask, firstTcs.Task, secondTcs.Task);

				/* Assert */
				Assert.True(true, "Published and subscribe sucessfull.");

			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Use_Priority()
		{
			/* Setup */
			using (var subscriber = BusClientFactory.CreateDefault())
			using (var publisher = BusClientFactory.CreateDefault())
			{
				var prioritySent = false;
				var queueBuilt = new TaskCompletionSource<bool>();
				var priorityTcs = new TaskCompletionSource<BasicMessage>();
				subscriber.SubscribeAsync<BasicMessage>(async (message, context) =>
				{
					await queueBuilt.Task;
					if (!prioritySent)
					{
						await subscriber.PublishAsync(new BasicMessage
						{
							Prop = "I am important!"
						}, configuration: cfg => cfg.WithProperties(p =>
						{
							p.Priority = 3;
						}));
						prioritySent = true;
					}
					else
					{
						priorityTcs.TrySetResult(message);
					}

				}, cfg => cfg
					.WithQueue(q => q.WithArgument(QueueArgument.MaxPriority, 3).WithAutoDelete())
					.WithSubscriberId("priority")
					.WithPrefetchCount(1));

				/* Test */
				await publisher.PublishAsync(new BasicMessage { Prop = "I will be delivered" });
				await publisher.PublishAsync(new BasicMessage { Prop = "Someone will pass me in the queue" }, configuration: cfg => cfg.WithProperties(p => p.Priority = 0));
				queueBuilt.SetResult(true);
				await priorityTcs.Task;

				/* Asset */
				Assert.Equal(expected: "I am important!", actual: priorityTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Stop_Subscribe_When_Subscription_Is_Disposed()
		{
			/* Setup */
			using (var publisher = BusClientFactory.CreateDefault())
			using (var subscriber = BusClientFactory.CreateDefault())
			{
				var firstMessage = new BasicMessage { Prop = "Value" };
				var secondMessage = new BasicMessage { Prop = "AnotherValue" };
				var firstRecievedTcs = new TaskCompletionSource<BasicMessage>();
				var secondRecievedTcs = new TaskCompletionSource<BasicMessage>();
				var recievedCount = 0;

				var subscription = subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					recievedCount++;
					if (!firstRecievedTcs.Task.IsCompleted)
					{
						firstRecievedTcs.SetResult(message);
					}
					return Task.FromResult(true);
				});

				/* Test */
				await publisher.PublishAsync(firstMessage);
				await firstRecievedTcs.Task;
				subscription.Dispose();
				var recievedAfterFirstPublish = recievedCount;
				await publisher.PublishAsync(secondMessage);
				await Task.Delay(20);
				publisher.SubscribeAsync<BasicMessage>((message, context) =>
				{
					secondRecievedTcs.SetResult(message);
					return Task.FromResult(true);
				});
				await secondRecievedTcs.Task;
				TestChannel.QueueDelete(subscription.QueueName);
				/* Assert */
				Assert.Equal(recievedAfterFirstPublish, recievedCount);
				Assert.Equal(firstRecievedTcs.Task.Result.Prop, firstMessage.Prop);
				Assert.Equal(secondRecievedTcs.Task.Result.Prop, secondMessage.Prop);
			}
		}
	}
}
