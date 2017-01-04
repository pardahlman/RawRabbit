using System;
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
				MessageContext recievedContext = null;
				await subscriber.SubscribeAsync<BasicMessage, MessageContext>((msg, context) =>
				{
					recievedContext = context;
					msgTcs.SetResult(msg);
					return Task.FromResult(0);
				});
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				publisher.PublishAsync(message);
				await msgTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, msgTcs.Task.Result.Prop);
				Assert.NotNull(recievedContext);
			}
		}
	}
}
