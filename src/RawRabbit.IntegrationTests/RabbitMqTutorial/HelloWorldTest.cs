using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.RabbitMqTutorial
{
	public class HelloWorldTest : IntegrationTestBase
	{
		public HelloWorldTest()
		{
			TestChannel.QueueDelete("hello");
		}

		public override void Dispose() 
		{
			TestChannel.QueueDelete("hello");
			base.Dispose();
		}

		[Fact]
		public async void Should_Support_The_Hello_World_Tutorial()
		{
			/* Setup */
			var sent = new BasicMessage { Prop = "Hello, world!" };
			var recieved = new TaskCompletionSource<BasicMessage>();
			var sender = BusClientFactory.CreateDefault();
			var reciever = BusClientFactory.CreateDefault();
			await reciever.SubscribeAsync<BasicMessage>((message, info) =>
			{
				recieved.SetResult(message);
				return Task.FromResult(true);
			}, configuration => configuration
					.WithQueue(queue =>
						queue
							.WithName("hello")
							.WithDurability(false)
							.WithExclusivity(false)
							.WithAutoDelete(false)
						)
			);

			/* Test */
			sender.PublishAsync(sent,
				builder => builder
					.WithQueue(queue =>
						queue.WithName("hello")
						.WithDurability(false)
						.WithExclusivity(false)
						.WithAutoDelete(false)
					)
			);
			await recieved.Task;

			/* Assert */
			Assert.Equal(sent.Prop, recieved.Task.Result.Prop);
		}
	}
}
