using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class MultipleRequestsTests
	{
		[Fact]
		public async void Should_Just_Work()
		{
			/* Setup */
			const int numberOfCalls = 1000;
			var bag = new ConcurrentBag<Guid>();
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault();
			await responder.RespondAsync<FirstRequest, FirstResponse>((req, i) =>
				Task.FromResult(new FirstResponse { Infered = Guid.NewGuid() })
			);

			/* Test */
			var sw = new Stopwatch();
			sw.Start();
			for (var i = 0; i < numberOfCalls; i++)
			{
				var response = await requester.RequestAsync<FirstRequest, FirstResponse>();
				bag.Add(response.Infered);
			}
			sw.Stop();

			/* Assert */
			Assert.Equal(numberOfCalls, bag.Count);
		}
	}
}
