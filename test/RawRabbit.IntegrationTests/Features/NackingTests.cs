using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Context;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class NackingTests : IntegrationTestBase
	{
		public NackingTests()
		{
			TestChannel.QueueDelete("basicmessage");
		}

		[Fact]
		public async Task Should_Be_Able_To_Nack_Message()
		{
			/* Setup */
			using (var firstResponder = TestClientFactory.CreateNormal<AdvancedMessageContext>())
			using (var secondResponder = TestClientFactory.CreateNormal<AdvancedMessageContext>())
			using (var requester = TestClientFactory.CreateNormal<AdvancedMessageContext>())
			{

				var hasBeenNacked = false;
				firstResponder.RespondAsync<BasicRequest, BasicResponse>((request, context) =>
				{
					BasicResponse response = null;
					if (!hasBeenNacked)
					{
						context?.Nack();
						hasBeenNacked = true;
					}
					else
					{
						response = new BasicResponse();
					}
					return Task.FromResult(response);
				}, c => c.WithNoAck(false));
				secondResponder.RespondAsync<BasicRequest, BasicResponse>((request, context) =>
				{
					BasicResponse response = null;
					if (!hasBeenNacked)
					{
						context?.Nack();
						hasBeenNacked = true;
					}
					else
					{
						response = new BasicResponse();
					}
					return Task.FromResult(response);
				}, c => c.WithNoAck(false));

				/* Test */
				var result = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(), configuration: cfg => cfg
						 .WithReplyQueue(
							 q => q.WithName("special_reply_queue")));

				/* Assert */
				Assert.NotNull(result);
				Assert.True(hasBeenNacked);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Nack_On_Subscribe()
		{
			/* Setup */
			using (var subscriber = TestClientFactory.CreateNormal<AdvancedMessageContext>())
			using (var secondSubscriber = TestClientFactory.CreateNormal<AdvancedMessageContext>())
			using (var publisher = TestClientFactory.CreateNormal<AdvancedMessageContext>())
			{
				var callcount = 0;
				var subscribeTcs = new TaskCompletionSource<bool>();
				var secondSubscribeTcs = new TaskCompletionSource<bool>();
				subscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					Interlocked.Increment(ref callcount);
					context?.Nack();
					subscribeTcs.TrySetResult(true);
					return Task.FromResult(true);
				});
				secondSubscriber.SubscribeAsync<BasicMessage>((message, context) =>
				{
					secondSubscribeTcs.TrySetResult(true);
					return Task.FromResult(true);
				});

				Task.WaitAll(
					publisher.PublishAsync<BasicMessage>(),
					subscribeTcs.Task,
					secondSubscribeTcs.Task
				);

				TestChannel.QueueDelete("basicmessage");

				Assert.Equal(expected: 1, actual: callcount);
			}
		}
	}
}
