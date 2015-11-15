using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class MultipleRequestsTests
	{
		[Fact]
		public async void Should_Just_Work()
		{
			/* Setup */
			const int numberOfCalls = 10000;
			var array = new Task[numberOfCalls];
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault();
			responder.RespondAsync<FirstRequest, FirstResponse>((req, i) =>
				Task.FromResult(new FirstResponse { Infered = Guid.NewGuid() })
			);

			/* Test */
			var sw = new Stopwatch();
			sw.Start();
			for (var i = 0; i < numberOfCalls; i++)
			{
				var response = requester.RequestAsync<FirstRequest, FirstResponse>();
				array[i] =response;
			}
			Task.WaitAll(array);
			sw.Stop();
			var ids = array
				.OfType<Task<FirstResponse>>()
				.Select(b => b.Result.Infered)
				.Where(id => id != Guid.Empty)
				.Distinct()
				.ToList();

			/* Assert */
			Assert.Equal(numberOfCalls, ids.Count);
		}
	}
}
