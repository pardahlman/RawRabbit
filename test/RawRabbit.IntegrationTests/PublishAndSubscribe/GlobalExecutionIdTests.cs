using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Testing.Abstractions;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Extensions;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class GlobalExecutionIdTests
	{
		[Fact]
		public async Task Should_Forward_On_Pub_Sub()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient())
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient())
			using (var thridSubscriber = RawRabbitFactory.CreateTestClient())
			using (var consumer = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var taskCompletionSources = new List<TaskCompletionSource<BasicDeliverEventArgs>>
				{
					new TaskCompletionSource<BasicDeliverEventArgs>(),
					new TaskCompletionSource<BasicDeliverEventArgs>(),
					new TaskCompletionSource<BasicDeliverEventArgs>()
				};
				await firstSubscriber.SubscribeAsync<FirstMessage>(message => firstSubscriber.PublishAsync(new SecondMessage()));
				await secondSubscriber.SubscribeAsync<SecondMessage>(message => secondSubscriber.PublishAsync(new ThirdMessage()));
				await thridSubscriber.SubscribeAsync<SecondMessage>(message => Task.FromResult(0));
				await consumer.BasicConsumeAsync(args =>
					{
						var tsc = taskCompletionSources.First(t => !t.Task.IsCompleted);
						tsc.TrySetResult(args);
						return Task.FromResult<Acknowledgement>(new Ack());
					}, cfg => cfg
					.Consume(c => c
						.OnExchange("rawrabbit.integrationtests.testmessages")
						.WithRoutingKey("#"))
					.FromDeclaredQueue(q => q
						.WithName("take_all")
						.WithAutoDelete())
				);

				/* Test */
				await publisher.PublishAsync(new FirstMessage());
				Task.WaitAll(taskCompletionSources.Select(t => t.Task).ToArray<Task>());
				
				var results = new List<string>();
				foreach (var tcs in taskCompletionSources)
				{
					var id = Encoding.UTF8.GetString(tcs.Task.Result.BasicProperties.Headers[PropertyHeaders.GlobalExecutionId] as byte[]);
					results.Add(id);
				}

				/* Assert */
				Assert.NotNull(results[0]);
				Assert.Equal(results[0], results[1]);
				Assert.Equal(results[1], results[2]);
			}
		}
	}
}
