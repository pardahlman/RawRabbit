using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;
using ExchangeType = RawRabbit.Configuration.Exchange.ExchangeType;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class PublishAndSubscribeTests : IntegrationTestBase
	{

		[Fact]
		public async Task Should_Be_Able_To_Subscribe_Without_Any_Additional_Config()
		{
			/* Setup */
			using (var publisher = TestClientFactory.CreateNormal())
			using (var subscriber = TestClientFactory.CreateNormal())
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
				});

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
			using (var subscriber = TestClientFactory.CreateNormal())
			using (var publisher = TestClientFactory.CreateNormal())
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

		[Fact]
		public void Should_Be_Able_To_Perform_Subscribe_For_Multiple_Types()
		{
			/* Setup */
			using (var subscriber = TestClientFactory.CreateNormal())
			using (var publisher = TestClientFactory.CreateNormal())
			{
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
		}

		[Fact]
		public async Task Should_Throw_Publish_Confirm_Exception_If_Server_Doesnt_Respond_Within_Time_Limit()
		{
			/* Setup */
			var publisher = TestClientFactory.CreateNormal(ioc => ioc.AddSingleton(p =>
			{
				var config = RawRabbitConfiguration.Local;
				config.PublishConfirmTimeout = TimeSpan.FromTicks(1);
				return config;
			}));
			using (publisher)
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
			using (var publisher = TestClientFactory.CreateNormal())
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
			using (var subscriber = TestClientFactory.CreateNormal())
			using (var publisher = TestClientFactory.CreateNormal())
			{
				var firstTcs = new TaskCompletionSource<bool>();
				var secondTcs = new TaskCompletionSource<bool>();
				subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					firstTcs.SetResult(true);
					return Task.FromResult(true);
				});
				subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					secondTcs.SetResult(true);
					return Task.FromResult(true);
				});

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
			using (var firstSubscriber = TestClientFactory.CreateNormal())
			using (var secondSubscriber = TestClientFactory.CreateNormal())
			using (var publisher = TestClientFactory.CreateNormal())
			{
				var firstTcs = new TaskCompletionSource<bool>();
				var secondTcs = new TaskCompletionSource<bool>();
				firstSubscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					firstTcs.SetResult(true);
					return Task.FromResult(true);
				}, cfg => cfg.WithSubscriberId("first_subscriber"));
				secondSubscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					secondTcs.SetResult(true);
					return Task.FromResult(true);
				}, cfg => cfg.WithSubscriberId("second_subscriber"));

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
			using (var subscriber = TestClientFactory.CreateNormal())
			using (var publisher = TestClientFactory.CreateNormal())
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
					.WithQueue(q => q.WithArgument(QueueArgument.MaxPriority, 3))
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
			using (var publisher = TestClientFactory.CreateNormal())
			using (var subscriber = TestClientFactory.CreateNormal())
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
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete(false)));

				/* Test */
				publisher.PublishAsync(firstMessage);
				await firstRecievedTcs.Task;
				subscription.Dispose();
				var recievedAfterFirstPublish = recievedCount;
				publisher.PublishAsync(secondMessage);
				await Task.Delay(20);
				publisher.SubscribeAsync<BasicMessage>((message, context) =>
				{
					secondRecievedTcs.SetResult(message);
					return Task.FromResult(true);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete(false)));
				await secondRecievedTcs.Task;
				TestChannel.QueueDelete(subscription.QueueName);
				/* Assert */
				Assert.Equal(recievedAfterFirstPublish, recievedCount);
				Assert.Equal(firstRecievedTcs.Task.Result.Prop, firstMessage.Prop);
				Assert.Equal(secondRecievedTcs.Task.Result.Prop, secondMessage.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Subscibe_To_Pure_Json_Message()
		{
			var conventions = new NamingConventions();
			using (var client = TestClientFactory.CreateNormal(ioc => ioc.AddSingleton<INamingConventions>(c => conventions)))
			{
				/* Setup */
				var tcs = new TaskCompletionSource<BasicMessage>();
				var subscription = client.SubscribeAsync<BasicMessage>((message, context) =>
				{
					tcs.TrySetResult(message);
					return Task.FromResult(true);
				});
				var uniqueValue = Guid.NewGuid().ToString();
				var jsonMsg = JsonConvert.SerializeObject(new BasicMessage { Prop = uniqueValue });

				/* Test */
				TestChannel.BasicPublish(
					conventions.ExchangeNamingConvention(typeof(BasicMessage)),
					conventions.QueueNamingConvention(typeof(BasicMessage)),
					true,
					null,
					Encoding.UTF8.GetBytes(jsonMsg));
				await tcs.Task;

				/* Assert */
				Assert.Equal(uniqueValue, tcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async void Should_Be_Able_To_Publish_Dynamic_Objects()
		{
			using (var client = TestClientFactory.CreateNormal())
			{
				/* Setup */
				var tcs = new TaskCompletionSource<DynamicMessage>();
				client.SubscribeAsync<DynamicMessage>((message, context) =>
				{
					tcs.TrySetResult(message);
					return Task.FromResult(true);
				});

				/* Test */
				client.PublishAsync(new DynamicMessage { Body = new { IsDynamic = true } });
				await tcs.Task;

				/* Assert */
				Assert.True(tcs.Task.Result.Body.IsDynamic);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Publish_Message_After_Failed_Publish()
		{
			using(var firstClient = TestClientFactory.CreateNormal())
			using (var secondClient = TestClientFactory.CreateNormal())
			{
				/* Setup */
				var tcs = new TaskCompletionSource<bool>();
				firstClient.SubscribeAsync<SimpleMessage>((message, context) =>
				{
					tcs.TrySetResult(true);
					return Task.FromResult(true);
				});

				/* Test */
				try
				{
					await
						secondClient.PublishAsync(new SimpleMessage(),
							configuration: cfg => cfg.WithExchange(e => e.WithType(ExchangeType.Direct)));
				}
				catch (Exception)
				{
					await Task.Delay(50);
					Assert.False(tcs.Task.IsCompleted);
				}
				secondClient.PublishAsync(new SimpleMessage());
				await tcs.Task;
				/* Assert */
				Assert.True(true);
			}
		}
	}
}

