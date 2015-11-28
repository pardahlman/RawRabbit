using System;
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
			var service = new ServiceCollection()
				.AddRawRabbit<AdvancedMessageContext>()
				.BuildServiceProvider();

			var subscriber = service.GetService<IBusClient<AdvancedMessageContext>>();
			var publisher = service.GetService<IBusClient<AdvancedMessageContext>>();

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
	}
}
