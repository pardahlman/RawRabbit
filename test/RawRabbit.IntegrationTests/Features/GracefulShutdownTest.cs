using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe.Extensions;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class GracefulShutdownTest : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Cancel_Subscription_When_Shutdown_Is_Called()
		{
			var singleton = vNext.Pipe.RawRabbitFactory.CreateSingleton();
			var instanceFactory = vNext.Pipe.RawRabbitFactory.CreateInstanceFactory();
			var client = instanceFactory.Create();
			var processTime = TimeSpan.FromMilliseconds(50);
			var firstMsg = (new BasicMessage { Prop = "I'll get processed" });
			var secondMsg = (new BasicMessage { Prop = "I'll get stuck in the queue" });

			var firstTsc = new TaskCompletionSource<BasicMessage>();
			await client.SubscribeAsync<BasicMessage>(async message =>
			{
				firstTsc.TrySetResult(message);
				await Task.Delay(processTime);
			});

			await client.PublishAsync(firstMsg);
			await firstTsc.Task;
			var shutdownTask = instanceFactory.ShutdownAsync(processTime);
			await singleton.PublishAsync(secondMsg);
			await shutdownTask;

			var secondRecieved = await singleton.GetAsync<BasicMessage>(get => get.WithNoAck());
			await singleton.DeleteQueueAsync<BasicMessage>();
			await singleton.DeleteExchangeAsync<BasicMessage>();
			singleton.Dispose();

			Assert.Equal(firstMsg.Prop, firstTsc.Task.Result.Prop);
			Assert.Equal(secondMsg.Prop, secondRecieved.Content.Prop);
		}
	}
}
