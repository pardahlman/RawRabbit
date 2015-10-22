using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.RabbitMqTutorial
{
	public class WorkQueueTest : IntegrationTestBase
	{
		public WorkQueueTest()
		{
			TestChannel.QueueDelete("task_queue");
		}

		public override void Dispose()
		{
			TestChannel.QueueDelete("task_queue");
			base.Dispose();
		}

		[Fact]
		public async void Should_Support_The_Worker_Queues_Tutorial()
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
					.WithPrefetchCount(1)
					.WithQueue(queue =>
						queue
							.WithName("task_queue")
							.WithDurability()
						)
			);

			/* Test */
			await sender.PublishAsync(sent,
				configuration: builder => builder
					.WithRoutingKey("task_queue")
					.WithQueue(queue =>
						queue
							.WithDurability()
					)
			);
			await recieved.Task;

			/* Assert */
			Assert.Equal(sent.Prop, recieved.Task.Result.Prop);
		}
	}
}
