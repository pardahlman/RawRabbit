using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
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
		public async Task Should_Support_The_Worker_Queues_Tutorial()
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
						.WithPrefetchCount(1)
						.WithQueue(queue =>
							queue
								.WithName("task_queue")
								.WithDurability()
							)
						.WithRoutingKey("task_queue")
				);

				/* Test */
				await sender.PublishAsync(sent,
					configuration: builder => builder
						.WithRoutingKey("task_queue")
				);
				await recieved.Task;

				/* Assert */
				Assert.Equal(expected: sent.Prop, actual: recieved.Task.Result.Prop);
			}
		}
	}
}
