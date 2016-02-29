using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
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
		public async Task Should_Support_The_Hello_World_Tutorial()
		{
			/* Setup */
			var sent = new BasicMessage { Prop = "Hello, world!" };
			var recieved = new TaskCompletionSource<BasicMessage>();
			var sender = BusClientFactory.CreateDefault();
			var reciever = BusClientFactory.CreateDefault();
			reciever.SubscribeAsync<BasicMessage>((message, info) =>
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
