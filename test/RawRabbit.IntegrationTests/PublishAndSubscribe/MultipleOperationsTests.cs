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
				var receivedCount = 0;
				const int sendCount = 2000;
				var publishTasks = new Task[sendCount];
				var receivedTcs = new TaskCompletionSource<int>();
				await subscriber.SubscribeAsync<BasicMessage>(received =>
				{
					Interlocked.Increment(ref receivedCount);
					if (receivedCount == sendCount)
					{
						receivedTcs.TrySetResult(receivedCount);
					}
					return Task.FromResult(true);
				});

				/* Test */
				for (var i = 0; i < sendCount; i++)
				{
					publishTasks[i] = publisher.PublishAsync(new BasicMessage());
				}
				Task.WaitAll(publishTasks);
				await receivedTcs.Task;
				/* Assert */
				Assert.Equal(receivedTcs.Task.Result, sendCount);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Perform_Multiple_Concurrent_Rpc()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				const int sendCount = 2000;
				var publishTasks = new Task[sendCount];
				await responder.RespondAsync<BasicRequest, BasicResponse>(received =>
					Task.FromResult(new BasicResponse())
				);

				/* Test */
				for (var i = 0; i < sendCount; i++)
				{
					publishTasks[i] = requester.RequestAsync<BasicRequest, BasicResponse>();
				}
				Task.WaitAll(publishTasks);

				/* Assert */
				Assert.True(true);
			}
		}
	}
}
