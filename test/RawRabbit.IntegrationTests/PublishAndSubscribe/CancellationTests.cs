using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class CancellationTests
	{
		[Fact]
		public async Task Should_Honor_Task_Cancellation()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage {Prop = Guid.NewGuid().ToString()};
				var receivedTcs = new TaskCompletionSource<BasicMessage>();
				var sendCts = new CancellationTokenSource();
				await subscriber.SubscribeAsync<BasicMessage>(received =>
				{
					if (received.Prop == message.Prop)
					{
						receivedTcs.TrySetResult(received);
					}
					return Task.FromResult(true);
				});

				/* Test */
				sendCts.CancelAfter(TimeSpan.FromTicks(400));
				var publishTask = publisher.PublishAsync(new BasicMessage(), token: sendCts.Token);
				receivedTcs.Task.Wait(100);

				/* Assert */
				Assert.False(receivedTcs.Task.IsCompleted, "Message was sent, even though execution was cancelled.");
				Assert.True(publishTask.IsCanceled, "The publish task should be cancelled.");
			}
		}
	}
}
