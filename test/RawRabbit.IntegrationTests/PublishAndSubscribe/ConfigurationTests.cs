using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class ConfigurationTests
	{
		[Fact]
		public async Task Should_Work_Without_Any_Additional_Configuration()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				await subscriber.SubscribeAsync<BasicMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(true);
				});
				var message = new BasicMessage {Prop = "Hello, world!"};

				/* Test */
				await publisher.PublishAsync(message);
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Honor_Exchange_Name_Configuration()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				await subscriber.SubscribeAsync<BasicMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(true);
				}, cfg => cfg.OnDeclaredExchange(e=> e.WithName("custom_exchange")));

				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.WithName("custom_exchange")));
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}
	}
}
