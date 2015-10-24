using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class TimeoutTests
	{
		[Fact]
		public async void Should_Interupt_Task_After_Timeout_Not_Met()
		{
			/* Setup */
			var responder = BusClientFactory.CreateDefault();
			var requester = BusClientFactory.CreateDefault(requestTimeout: TimeSpan.FromMilliseconds(200));
			await responder.RespondAsync<FirstRequest, FirstResponse>((request, context) =>
			{
				return Task
					.Run(() => Task.Delay(250))
					.ContinueWith(t => new FirstResponse());
			});

			/* Test */
			/* Assert */
			await Assert.ThrowsAsync<TimeoutException>(() => requester.RequestAsync<FirstRequest, FirstResponse>());
		}

		[Fact]
		public async void Should_Not_Throw_If_Response_Is_Handled_Within_Time_Limit()
		{
			/* Setup */
			var responder = BusClientFactory.CreateDefault();
			var requester = BusClientFactory.CreateDefault(requestTimeout: TimeSpan.FromMilliseconds(200));
			await responder.RespondAsync<FirstRequest, FirstResponse>((request, context) =>
			{
				return Task.FromResult(new FirstResponse());
			});

			/* Test */
			await requester.RequestAsync<FirstRequest, FirstResponse>();
		
			/* Assert */
			Assert.True(true, "Response recieved without throwing.");
		}
	}
}
