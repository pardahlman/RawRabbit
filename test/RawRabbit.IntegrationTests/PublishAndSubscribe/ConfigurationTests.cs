using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
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
				}, ctx => ctx
					.ConsumerConfiguration(cfg => cfg
						.OnDeclaredExchange(e=> e
							.WithName("custom_exchange")
						))
				);

				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message, cfg => cfg.OnExchange("custom_exchange"));
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Honor_Complex_Configuration()
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
				}, ctx => ctx
					.ConsumerConfiguration(cfg => cfg
						.Consume(c => c
							.WithRoutingKey("custom_key")
							.WithConsumerTag("custom_tag")
							.WithPrefetchCount(2)
							.WithNoLocal(false))
						.FromDeclaredQueue(q => q
							.WithName("custom_queue")
							.WithAutoDelete()
							.WithArgument(QueueArgument.DeadLetterExchange, "dlx"))
						.OnDeclaredExchange(e=> e
							.WithName("custom_exchange")
							.WithType(ExchangeType.Topic))
				));

				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message, cfg => cfg
						.OnExchange("custom_exchange")
						.WithRoutingKey("custom_key")
				);
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}
	}
}
