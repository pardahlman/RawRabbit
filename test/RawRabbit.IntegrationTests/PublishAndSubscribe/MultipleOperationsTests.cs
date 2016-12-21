using System.Threading;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class MultipleOperationsTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Perform_Multiple_Concurrent_Publishes()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedCount = 0;
				const int sendCount = 2000;
				var publishTasks = new Task[sendCount];
				var recievedTcs = new TaskCompletionSource<int>();
				await subscriber.SubscribeAsync<BasicMessage>(recieved =>
				{
					Interlocked.Increment(ref recievedCount);
					if (recievedCount == sendCount)
					{
						recievedTcs.TrySetResult(recievedCount);
					}
					return Task.FromResult(true);
				});

				/* Test */
				for (var i = 0; i < sendCount; i++)
				{
					publishTasks[i] = publisher.PublishAsync(new BasicMessage());
				}
				Task.WaitAll(publishTasks);
				await recievedTcs.Task;
				/* Assert */
				Assert.Equal(recievedTcs.Task.Result, sendCount);
			}
		}
	}
}
