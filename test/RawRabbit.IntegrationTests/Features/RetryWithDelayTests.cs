using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Context;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class RetryWithDelayTests
	{
		[Fact]
		public async Task Should_Retry_For_Publish_Subscribe_After_Given_Timespan()
		{
			/* Setup */
			var subscriber = BusClientFactory.CreateDefault<AdvancedMessageContext>();
			var publisher = BusClientFactory.CreateDefault<AdvancedMessageContext>();

			var subscribeTcs = new TaskCompletionSource<bool>();
			var deplay = TimeSpan.FromSeconds(1);
			var hasBeenDelayed = false;
			var firstRecieved = DateTime.MinValue;
			var secondRecieved = DateTime.MinValue;

			subscriber.SubscribeAsync<BasicMessage>((message, context) =>
			{
				if (!hasBeenDelayed)
				{
					firstRecieved = DateTime.Now;
					context.RetryLater(deplay);
					hasBeenDelayed = true;
					return Task.FromResult(true);
				}
				secondRecieved = DateTime.Now;
				return Task.Delay(10).ContinueWith(t => subscribeTcs.SetResult(true));
			});

			/* Test */
			await publisher.PublishAsync(new BasicMessage { Prop = "I'm about to be reborn!"});
			await subscribeTcs.Task;
			var actualDelay = secondRecieved - firstRecieved;
			
			/* Assert */
			Assert.Equal(actualDelay.Seconds, deplay.Seconds);
		}

		[Fact]
		public async Task Should_Retry_And_Leave_Requester_Hanging_On_Rpc()
		{
			var requester = BusClientFactory.CreateDefault<AdvancedMessageContext>();
			var responder = BusClientFactory.CreateDefault<AdvancedMessageContext>();

			var delay = TimeSpan.FromSeconds(1);
			var hasBeenDelayed = false;
			var firstRecieved = DateTime.MinValue;
			var secondRecieved = DateTime.MinValue;

			responder.RespondAsync<BasicRequest, BasicResponse>((request, context) =>
			{
				if (!hasBeenDelayed)
				{
					firstRecieved = DateTime.Now;
					hasBeenDelayed = true;
					context.RetryLater(delay);
					return Task.FromResult<BasicResponse>(null);
				}
				secondRecieved = DateTime.Now;
				return Task.FromResult(new BasicResponse());
			});

			/* Test */
			var response = await requester.RequestAsync<BasicRequest, BasicResponse>();
			var actualDelay = secondRecieved - firstRecieved;

			/* Assert */
			Assert.Equal(actualDelay.Seconds, delay.Seconds);
		}
	}
}
