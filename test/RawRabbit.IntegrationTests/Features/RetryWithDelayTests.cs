using System;
using System.Collections.Generic;
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
			using (var subscriber = BusClientFactory.CreateDefault<AdvancedMessageContext>())
			using (var publisher = BusClientFactory.CreateDefault<AdvancedMessageContext>())
			{
				var subscribeTcs = new TaskCompletionSource<bool>();
				var delay = TimeSpan.FromSeconds(1);
				var hasBeenDelayed = false;
				var firstRecieved = DateTime.MinValue;
				var secondRecieved = DateTime.MinValue;

				subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					if (!hasBeenDelayed)
					{
						firstRecieved = DateTime.Now;
						context.RetryLater(delay);
						hasBeenDelayed = true;
						return Task.FromResult(true);
					}
					secondRecieved = DateTime.Now;
					return Task.Delay(10).ContinueWith(t => subscribeTcs.SetResult(true));
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				await publisher.PublishAsync(new BasicMessage { Prop = "I'm about to be reborn!" });
				await subscribeTcs.Task;
				var actualDelay = secondRecieved - firstRecieved;
				await Task.Delay(80);

				/* Assert */
				Assert.Equal(expected: delay.Seconds, actual: actualDelay.Seconds);
			}
		}

		[Fact]
		public async Task Should_Retry_And_Leave_Requester_Hanging_On_Rpc()
		{
			using (var requester = BusClientFactory.CreateDefault<AdvancedMessageContext>())
			using (var responder = BusClientFactory.CreateDefault<AdvancedMessageContext>())
			{
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
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				var response = await requester.RequestAsync<BasicRequest, BasicResponse>();
				var actualDelay = secondRecieved - firstRecieved;
				await Task.Delay(80);

				/* Assert */
				Assert.Equal(expected: delay.Seconds, actual: actualDelay.Seconds);
			}
		}

		[Fact]
		public async Task Should_Successfully_Retry_With_Different_TimeSpans()
		{
			/* Setup */
			using (var subscriber = BusClientFactory.CreateDefault<AdvancedMessageContext>())
			using (var publisher = BusClientFactory.CreateDefault<AdvancedMessageContext>())
			{
				var recived = new List<DateTime>();
				var redelivered = new List<DateTime>();
				var allRedelivered = new TaskCompletionSource<bool>();
				var recievedCount = 0;

				subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					recievedCount++;
					if (recievedCount <= 3)
					{
						recived.Add(DateTime.Now);
						context.RetryLater(TimeSpan.FromSeconds(recievedCount));
					}
					else
					{
						redelivered.Add(DateTime.Now);
						if (redelivered.Count == 3)
						{
							allRedelivered.SetResult(true);
						}
					}
					return Task.FromResult(true);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				await publisher.PublishAsync(new BasicMessage { Prop = "I'm about to be reborn!" });
				await publisher.PublishAsync(new BasicMessage { Prop = "I'm about to be reborn!" });
				await publisher.PublishAsync(new BasicMessage { Prop = "I'm about to be reborn!" });
				await allRedelivered.Task;

				/* Assert */
				for (var i = 0; i < 3; i++)
				{
					Assert.Equal(expected: (redelivered[i]-recived[i]).Seconds, actual: i+1);
				}
			}
		}
	}
}
