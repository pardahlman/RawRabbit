using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class ConsumeThrottleTests
	{
		[Fact]
		public async Task Should_Throttle_With_Provided_Semaphore()
		{
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			using (var publisher = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				const int messageCount = 6;
				const int concurrencyLevel = 2;
				var doneTsc = new TaskCompletionSource<bool>();
				var concurrentEntryTimes = new ConcurrentQueue<DateTime>();
				var concurrentExitTimes = new ConcurrentQueue<DateTime>();
				await subscriber.SubscribeAsync<BasicMessage>(async message =>
				{
					concurrentEntryTimes.Enqueue(DateTime.Now);
					await Task.Delay(TimeSpan.FromMilliseconds(concurrencyLevel/messageCount * 100));
					concurrentExitTimes.Enqueue(DateTime.Now);
					if (concurrentExitTimes.Count == messageCount)
					{
						doneTsc.TrySetResult(true);
					}
				}, context: c => c.ConsumerConcurrency(concurrencyLevel));

				/* Test */
				for (var i = 0; i < messageCount; i++)
				{
					await publisher.PublishAsync(new BasicMessage());
				}
				await doneTsc.Task;

				/* Assert */
				var entryTimes = concurrentEntryTimes.ToList();
				var exitTimes = concurrentExitTimes.ToList();
				for (var i = concurrencyLevel; i < messageCount-1; i++)
				{
					var timeDiff = entryTimes[i] - exitTimes[i - 1];
					Assert.True(timeDiff.TotalMilliseconds > 0, $"Entry {entryTimes[i]} is before previous exit {exitTimes[i - 1]}");
				}
			}
		}

		[Fact]
		public async Task Should_Not_Throttle_If_No_Semaphore_Provided()
		{
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			using (var publisher = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				const int messageCount = 6;
				var doneTsc = new TaskCompletionSource<bool>();
				var concurrentEntryTimes = new ConcurrentQueue<DateTime>();
				var concurrentExitTimes = new ConcurrentQueue<DateTime>();
				await subscriber.SubscribeAsync<BasicMessage>(async message =>
				{
					concurrentEntryTimes.Enqueue(DateTime.Now);
					await Task.Delay(TimeSpan.FromMilliseconds(200));
					concurrentExitTimes.Enqueue(DateTime.Now);
					if (concurrentExitTimes.Count == messageCount)
					{
						doneTsc.TrySetResult(true);
					}
				});

				/* Test */
				for (var i = 0; i < messageCount; i++)
				{
					await publisher.PublishAsync(new BasicMessage());
				}
				await doneTsc.Task;

				/* Assert */
				var entryTimes = concurrentEntryTimes.ToList();
				var exitTimes = concurrentExitTimes.ToList();
				for (var i = 1; i < messageCount - 1; i++)
				{
					Assert.True(
						entryTimes[i] < exitTimes[i - 1],
						$"Expected exit {exitTimes[i - 1]} to occure after entry {entryTimes[i]}, since execution is not throttled."
					);
				}
			}
		}
	}
}
