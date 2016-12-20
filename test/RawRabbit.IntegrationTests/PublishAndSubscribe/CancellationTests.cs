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
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				var sendCts = new CancellationTokenSource();
				await subscriber.SubscribeAsync<BasicMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(true);
				});

				/* Test */
				var publishTask = publisher.PublishAsync(new BasicMessage(), token: sendCts.Token);
				sendCts.CancelAfter(20);
				recievedTcs.Task.Wait(100);

				/* Assert */
				Assert.False(recievedTcs.Task.IsCompleted);
				Assert.True(publishTask.IsCanceled);
			}
		}
	}
}
