using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.IntegrationTests.TestMessages.Extras;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class AcknowledgementSubscribeTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Auto_Ack()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				await subscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					recievedTcs.TrySetResult(recieved);
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Ack()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				await subscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return new Ack();
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Ack_From_Subscriber_With_Context()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				await subscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					recievedTcs.TrySetResult(recieved);
					return new Ack();
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Nack_Without_Requeue()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient())
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstSubscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					firstTsc.TrySetResult(recieved);
					return new Nack(requeue: false);
				});
				await secondSubscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					secondTsc.TrySetResult(recieved);
					return new Nack(requeue: false);
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				Task.WaitAll(new [] {firstTsc.Task, secondTsc.Task}, TimeSpan.FromMilliseconds(200));

				/* Assert */
				Assert.Equal(message.Prop, firstTsc.Task.Result.Prop);
				Assert.False(secondTsc.Task.IsCompleted);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Nack_Without_Requeue_From_Handler_With_Context()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient())
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstSubscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					firstTsc.TrySetResult(recieved);
					return new Nack(requeue: false);
				});
				await secondSubscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					secondTsc.TrySetResult(recieved);
					return new Nack(requeue: false);
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				Task.WaitAll(new[] { firstTsc.Task, secondTsc.Task }, TimeSpan.FromMilliseconds(200));

				/* Assert */
				Assert.Equal(message.Prop, firstTsc.Task.Result.Prop);
				Assert.False(secondTsc.Task.IsCompleted);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Nack_With_Requeue()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient())
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstSubscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					firstTsc.TrySetResult(recieved);
					return new Nack();
				});
				await secondSubscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					secondTsc.TrySetResult(recieved);
					return new Ack();
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				Task.WaitAll(firstTsc.Task, secondTsc.Task);

				/* Assert */
				Assert.Equal(message.Prop, firstTsc.Task.Result.Prop);
				Assert.Equal(message.Prop, secondTsc.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Nack_With_Requeue_From_Subscriber_With_Context()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient())
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstSubscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					firstTsc.TrySetResult(recieved);
					return new Nack();
				});
				await secondSubscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					secondTsc.TrySetResult(recieved);
					return new Ack();
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				Task.WaitAll(firstTsc.Task, secondTsc.Task);

				/* Assert */
				Assert.Equal(message.Prop, firstTsc.Task.Result.Prop);
				Assert.Equal(message.Prop, secondTsc.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Reject_With_Requeue()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient())
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstSubscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					firstTsc.TrySetResult(recieved);
					return new Reject();
				});
				await secondSubscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					secondTsc.TrySetResult(recieved);
					return new Ack();
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				Task.WaitAll(firstTsc.Task, secondTsc.Task);

				/* Assert */
				Assert.Equal(message.Prop, firstTsc.Task.Result.Prop);
				Assert.Equal(message.Prop, secondTsc.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Reject_With_Requeue_From_Subscriber_With_Context()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient())
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstSubscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					firstTsc.TrySetResult(recieved);
					return new Reject();
				});
				await secondSubscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					secondTsc.TrySetResult(recieved);
					return new Ack();
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				Task.WaitAll(firstTsc.Task, secondTsc.Task);

				/* Assert */
				Assert.Equal(message.Prop, firstTsc.Task.Result.Prop);
				Assert.Equal(message.Prop, secondTsc.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Retry()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseRetryLater()}))
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<DateTime>();
				var secondTsc = new TaskCompletionSource<DateTime>();
				await firstSubscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					firstTsc.TrySetResult(DateTime.Now);
					return Retry.In(TimeSpan.FromSeconds(1));
				});
				await secondSubscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					secondTsc.TrySetResult(DateTime.Now);
					return new Ack();
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				Task.WaitAll(firstTsc.Task, secondTsc.Task);

				/* Assert */
				Assert.Equal(1, (secondTsc.Task.Result - firstTsc.Task.Result).Seconds);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Retry_From_Subscriber_With_Context()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient(p => p.UseRetryLater()))
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<DateTime>();
				var secondTsc = new TaskCompletionSource<DateTime>();
				await firstSubscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					firstTsc.TrySetResult(DateTime.Now);
					return Retry.In(TimeSpan.FromSeconds(1));
				});
				await secondSubscriber.SubscribeAsync<BasicMessage, MessageContext>(async (recieved, context) =>
				{
					secondTsc.TrySetResult(DateTime.Now);
					return new Ack();
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				Task.WaitAll(firstTsc.Task, secondTsc.Task);

				/* Assert */
				Assert.Equal(1, (secondTsc.Task.Result - firstTsc.Task.Result).Seconds);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Retry_Multiple_Times()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient(p => p.UseRetryLater()))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<DateTime>();
				var secondTsc = new TaskCompletionSource<DateTime>();
				var thirdTsc = new TaskCompletionSource<DateTime>();
				await subscriber.SubscribeAsync<BasicMessage>(async recieved =>
				{
					var recievedAt = DateTime.Now;
					if (firstTsc.TrySetResult(recievedAt))
					{
						return Retry.In(TimeSpan.FromSeconds(1));
					}
					if (secondTsc.TrySetResult(recievedAt))
					{
						return Retry.In(TimeSpan.FromSeconds(1));
					}
					thirdTsc.TrySetResult(recievedAt);
					return new Ack();
				});

				/* Test */
				await publisher.PublishAsync(new BasicMessage { Prop = "Hello, world!" });
				Task.WaitAll(firstTsc.Task, secondTsc.Task, thirdTsc.Task);

				/* Assert */
				Assert.Equal(1, (secondTsc.Task.Result - firstTsc.Task.Result).Seconds);
				Assert.Equal(1, (thirdTsc.Task.Result - secondTsc.Task.Result).Seconds);
			}
		}

		[Fact]
		public async Task Should_Handle_Concurrent_Retries()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient(p => p.UseRetryLater()))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<DateTime>();
				var secondTsc = new TaskCompletionSource<DateTime>();
				var thirdTsc = new TaskCompletionSource<DateTime>();
				var forthTsc = new TaskCompletionSource<DateTime>();

				await subscriber.SubscribeAsync<BasicMessage> (async recieved =>
				{
					var recievedAt = DateTime.Now;
					if (firstTsc.TrySetResult(recievedAt))
					{
						await Task.Delay(TimeSpan.FromMilliseconds(100));
						subscriber.PublishAsync(new NamespacedMessages());
						return Retry.In(TimeSpan.FromSeconds(1));
					}
					thirdTsc.TrySetResult(recievedAt);
					return new Ack();
				});
				await subscriber.SubscribeAsync<NamespacedMessages>(async second =>
				{
					var recievedAt = DateTime.Now;
					if (secondTsc.TrySetResult(recievedAt))
						 {
						return Retry.In(TimeSpan.FromSeconds(1));
					}
					forthTsc.TrySetResult(recievedAt);
					return new Ack();
				});

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				Task.WaitAll(firstTsc.Task, secondTsc.Task, thirdTsc.Task, forthTsc.Task);

				/* Assert */
				Assert.Equal(1, (thirdTsc.Task.Result - firstTsc.Task.Result).Seconds);
				Assert.Equal(1, (forthTsc.Task.Result - secondTsc.Task.Result).Seconds);
			}
		}
	}
}
