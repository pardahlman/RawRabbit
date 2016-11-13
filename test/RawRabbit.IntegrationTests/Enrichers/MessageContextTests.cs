using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class MessageContextTests
	{
		[Fact]
		public async Task Should_Send_Context_On_Rpc()
		{
			using (var requester = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.RequestMessageContext<MessageContext>()}))
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				MessageContext recievedContext = null;
				await responder.RespondAsync<BasicRequest, BasicResponse, MessageContext>((request, context) =>
					{
						recievedContext = context;
						return Task.FromResult(new BasicResponse());
					}
				);

				/* Test */
				await requester.RequestAsync<BasicRequest, BasicResponse>();

				/* Assert */
				Assert.NotNull(recievedContext);
			}
		}

		[Fact]
		public async Task Should_Implicitly_Send_Context_On_Pub_Sub()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.PublishMessageContext<MessageContext>() }))
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var contextTsc = new TaskCompletionSource<MessageContext>();
				await subscriber.SubscribeAsync<BasicMessage, MessageContext>((request, context) =>
				{
					contextTsc.TrySetResult(context);
					return Task.FromResult(0);
				});

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await contextTsc.Task;
				/* Assert */
				Assert.NotNull(contextTsc.Task);
			}
		}

		[Fact]
		public async Task Should_Override_With_Explicit_Context_On_Pub_Sub()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.PublishMessageContext<MessageContext>() }))
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var contextTsc = new TaskCompletionSource<IMessageContext>();
				await subscriber.SubscribeAsync<BasicMessage, IMessageContext>((request, context) =>
				{
					contextTsc.TrySetResult(context);
					return Task.FromResult(0);
				});

				/* Test */
				await publisher.PublishAsync(new BasicMessage(), new TestMessageContext());
				await contextTsc.Task;
				/* Assert */
				Assert.IsType<TestMessageContext>(contextTsc.Task.Result);
			}
		}
	}
}
