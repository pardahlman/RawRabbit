using System.Threading.Tasks;
using RawRabbit.Enrichers.QueueSuffix;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
using RawRabbit.vNext.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class QueueSuffixTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Create_Unique_Queue_With_Application_Name()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient())
			using (var secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseApplicationQueueSuffix()
			}))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstClient.SubscribeAsync<BasicMessage>(message =>
				{
					firstTsc.TrySetResult(message);
					return Task.FromResult(0);
				});
				await secondClient.SubscribeAsync<BasicMessage>(message =>
				{
					secondTsc.TrySetResult(message);
					return Task.FromResult(0);
				});
				
				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await firstTsc.Task;
				await secondTsc.Task;

				/* Assert */
				Assert.True(true,"Should be delivered to both subscribers");
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Disable_Application_Suffix()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseApplicationQueueSuffix()
			}))
			using (var secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseApplicationQueueSuffix()
			}))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstClient.SubscribeAsync<BasicMessage>(message =>
				{
					firstTsc.TrySetResult(message);
					return Task.FromResult(0);
				});
				await secondClient.SubscribeAsync<BasicMessage>(message =>
				{
					secondTsc.TrySetResult(message);
					return Task.FromResult(0);
				}, ctx => ctx.UseApplicationQueueSuffix(false));

				/* Test */
			await publisher.PublishAsync(new BasicMessage());
				await firstTsc.Task;
				await secondTsc.Task;

				/* Assert */
				Assert.True(true, "Should be delivered to both subscribers");
			}
		}

		[Fact]
		public async Task Should_Append_Application_Name_In_Combination_With_Other_Suffix()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient())
			using (var secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseApplicationQueueSuffix()
			}))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstClient.SubscribeAsync<BasicMessage>(message =>
				{
					firstTsc.TrySetResult(message);
					return Task.FromResult(0);
				}, ctx => ctx
					.UseConsumerConfiguration(cfg => cfg
						.FromDeclaredQueue(q => q
							.WithNameSuffix("custom"))));
				await secondClient.SubscribeAsync<BasicMessage>(message =>
				{
					secondTsc.TrySetResult(message);
					return Task.FromResult(0);
				}, ctx => ctx
					.UseConsumerConfiguration(cfg => cfg
						.FromDeclaredQueue(q => q
							.WithNameSuffix("custom"))));

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await firstTsc.Task;
				await secondTsc.Task;

				/* Assert */
				Assert.True(true, "Should be delivered to both subscribers");
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Create_Unique_Queue_With_Host_Name()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient())
			using (var secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseHostQueueSuffix()
			}))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstClient.SubscribeAsync<BasicMessage>(message =>
				{
					firstTsc.TrySetResult(message);
					return Task.FromResult(0);
				});
				await secondClient.SubscribeAsync<BasicMessage>(message =>
				{
					secondTsc.TrySetResult(message);
					return Task.FromResult(0);
				});

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await firstTsc.Task;
				await secondTsc.Task;

				/* Assert */
				Assert.True(true, "Should be delivered to both subscribers");
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Disable_Host_Name_Suffix()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseHostQueueSuffix()
			}))
			using (var secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseHostQueueSuffix()
			}))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstClient.SubscribeAsync<BasicMessage>(message =>
				{
					firstTsc.TrySetResult(message);
					return Task.FromResult(0);
				});
				await secondClient.SubscribeAsync<BasicMessage>(message =>
				{
					secondTsc.TrySetResult(message);
					return Task.FromResult(0);
				}, ctx => ctx.UseHostnameQueueSuffix(false));

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await firstTsc.Task;
				await secondTsc.Task;

				/* Assert */
				Assert.True(true, "Should be delivered to both subscribers");
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Create_Unique_Queue_With_Custom_Suffix()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient())
			using (var secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseCustomQueueSuffix("custom")
			}))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstClient.SubscribeAsync<BasicMessage>(message =>
				{
					firstTsc.TrySetResult(message);
					return Task.FromResult(0);
				});
				await secondClient.SubscribeAsync<BasicMessage>(message =>
				{
					secondTsc.TrySetResult(message);
					return Task.FromResult(0);
				});

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await firstTsc.Task;
				await secondTsc.Task;

				/* Assert */
				Assert.True(true, "Should be delivered to both subscribers");
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Combine_Suffix()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient())
			using (var secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseApplicationQueueSuffix()
					.UseHostQueueSuffix()
					.UseCustomQueueSuffix("custom")
			}))
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicMessage>();
				var secondTsc = new TaskCompletionSource<BasicMessage>();
				await firstClient.SubscribeAsync<BasicMessage>(message =>
				{
					firstTsc.TrySetResult(message);
					return Task.FromResult(0);
				});
				await secondClient.SubscribeAsync<BasicMessage>(message =>
				{
					secondTsc.TrySetResult(message);
					return Task.FromResult(0);
				}, ctx => ctx
					.UseConsumerConfiguration(cfg => cfg
						.FromDeclaredQueue(q => q
							.WithNameSuffix("special"))));

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await firstTsc.Task;
				await secondTsc.Task;

				/* Assert */
				Assert.True(true, "Should be delivered to both subscribers");
			}
		}
	}
}
