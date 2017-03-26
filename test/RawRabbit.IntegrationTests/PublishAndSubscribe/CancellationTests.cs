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
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				var sendCts = new CancellationTokenSource();
				await subscriber.SubscribeAsync<BasicMessage>(recieved =>
				{
					if (recieved.Prop == message.Prop)
					{
						recievedTcs.TrySetResult(recieved);
					}
					return Task.FromResult(true);
				});

				/* Test */
				sendCts.CancelAfter(TimeSpan.FromTicks(400));
				var publishTask = publisher.PublishAsync(new BasicMessage(), token: sendCts.Token);
				recievedTcs.Task.Wait(100);

				/* Assert */
				Assert.False(recievedTcs.Task.IsCompleted, "Message was sent, even though execution was cancelled.");
				Assert.True(publishTask.IsCanceled, "The publish task should be cancelled.");
			}
		}
	}
}
