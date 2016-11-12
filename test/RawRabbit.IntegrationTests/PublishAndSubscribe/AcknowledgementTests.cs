using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class AcknowledgementTests
	{
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
		public async Task Should_Be_Able_To_Return_Retry()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient())
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
	}
}
