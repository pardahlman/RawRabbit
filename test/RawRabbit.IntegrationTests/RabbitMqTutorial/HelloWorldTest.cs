using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.RabbitMqTutorial
{
	public class HelloWorldTest : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Support_The_Hello_World_Tutorial()
		{
			/* Setup */
			using (var sender = TestClientFactory.CreateNormal())
			using (var reciever = TestClientFactory.CreateNormal())
			{
				var sent = new BasicMessage { Prop = "Hello, world!" };
				var recieved = new TaskCompletionSource<BasicMessage>();

				reciever.SubscribeAsync<BasicMessage>((message, info) =>
				{
					recieved.SetResult(message);
					return Task.FromResult(true);
				}, configuration => configuration
						.WithQueue(queue =>
							queue
								.WithName("hello")
								.WithExclusivity(false)
								.WithAutoDelete(false)
							)
						.WithRoutingKey("hello")
				);

				/* Test */
				await sender.PublishAsync(sent,
					configuration: builder => builder
						.WithRoutingKey("hello")
				);
				await recieved.Task;

				/* Assert */
				Assert.Equal(expected: sent.Prop, actual: recieved.Task.Result.Prop);
			}
		}
	}
}
