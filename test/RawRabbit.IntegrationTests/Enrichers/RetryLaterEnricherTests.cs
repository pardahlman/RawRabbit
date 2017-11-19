using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Enrichers.MessageContext.Subscribe;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class RetryLaterEnricherTests
	{
		[Fact]
		public async Task Should_Update_Retry_Information_Correctly()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient(p => p.UseRetryLater()))
			{
				/* Setup */
				var firstTcs = new TaskCompletionSource<RetryMessageContext>();
				var secondTcs = new TaskCompletionSource<RetryMessageContext>();
				var thirdTcs = new TaskCompletionSource<RetryMessageContext>();
				await subscriber.SubscribeAsync<BasicMessage, RetryMessageContext>(async (message, context) =>
				{
					if (!firstTcs.Task.IsCompleted)
					{
						firstTcs.TrySetResult(context);
						return Retry.In(TimeSpan.FromMilliseconds(200));
					}
					if (!secondTcs.Task.IsCompleted)
					{
						secondTcs.TrySetResult(context);
						return Retry.In(TimeSpan.FromMilliseconds(200));
					}
					thirdTcs.TrySetResult(context);
					return new Ack();
				}, ctx => ctx.UseMessageContext(c => new RetryMessageContext { RetryInfo = c.GetRetryInformation()}));

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await thirdTcs.Task;

				/* Assert */
				Assert.Equal(0, firstTcs.Task.Result.RetryInfo.NumberOfRetries);
				Assert.Equal(1, secondTcs.Task.Result.RetryInfo.NumberOfRetries);
				Assert.Equal(2, thirdTcs.Task.Result.RetryInfo.NumberOfRetries);
				Assert.Equal(secondTcs.Task.Result.RetryInfo.OriginalDelivered, thirdTcs.Task.Result.RetryInfo.OriginalDelivered);
			}
		}

		internal class RetryMessageContext
		{
			public RetryInformation RetryInfo { get; set; }
		}
	}
}
