using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.MessageContextArgument
{
	public class MessageContextForPubSub
	{
		[Fact]
		public async Task Should_Work_With_Default_Context()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.PublishMessageContext<Context.MessageContext>()}))
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var msgTcs = new TaskCompletionSource<BasicMessage>();
				var ctxTcs = new TaskCompletionSource<MessageContext>();
				await subscriber.SubscribeAsync<BasicMessage, MessageContext>((msg, context) =>
				{
					msgTcs.TrySetResult(msg);
					ctxTcs.TrySetResult(context);
					return Task.FromResult(0);
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message);
				await msgTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, msgTcs.Task.Result.Prop);
				Assert.NotNull(ctxTcs.Task.Result);
			}
		}
	}
}
