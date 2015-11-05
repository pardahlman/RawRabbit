using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class WorkerQueuesTest : IntegrationTestBase
	{
		[Fact]
		public async void Should_Call_Handle_Method_Just_As_Many_Times_As_Published()
		{
			/* Setup */
			var firstWorker = BusClientFactory.CreateDefault();
			var secondWorker = BusClientFactory.CreateDefault();
			var publisher = BusClientFactory.CreateDefault();

			var allCallTcs = new TaskCompletionSource<int>();
			const int noOfPublishes = 8;
			var firstWorkerCalls = 0;
			var secondWorkerCalls = 0;

			firstWorker.SubscribeAsync<BasicMessage>((msg, i) =>
			{
				firstWorkerCalls++;
				if (firstWorkerCalls + secondWorkerCalls == noOfPublishes)
				{
					allCallTcs.SetResult(noOfPublishes);
				}
				return Task.FromResult(true);
			}, cfg => cfg.WithPrefetchCount(1));
			secondWorker.SubscribeAsync<BasicMessage>((msg, i) =>
			{
				secondWorkerCalls++;
				if (firstWorkerCalls + secondWorkerCalls == noOfPublishes)
				{
					allCallTcs.SetResult(noOfPublishes);
				}
				return allCallTcs.Task;
			}, cfg => cfg.WithPrefetchCount(1));

			/* Test */
			for (var i = 0; i < noOfPublishes; i++)
			{
				publisher.PublishAsync(new BasicMessage());
			}
			await allCallTcs.Task;

			/* Assert */
			Assert.Equal(firstWorkerCalls + secondWorkerCalls, noOfPublishes);
			Assert.NotEqual(firstWorkerCalls, 0);
			Assert.NotEqual(secondWorkerCalls, 0);
		}

		public override void Dispose()
		{
			TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages");
			TestChannel.QueueDelete("basicmessage");
			base.Dispose();
		}
	}
}
