using System;
using System.Threading.Tasks;
using RawRabbit.Compatibility.Legacy.Configuration.Respond;
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

		[Fact]
		public async Task Should_Be_Able_To_Override_Custom_Suffix()
		{
			var customSuffix = new RawRabbitOptions
			{
				Plugins = p => p.UseCustomQueueSuffix("custom")
			};
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient(customSuffix))
			using (var secondClient = RawRabbitFactory.CreateTestClient(customSuffix))
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
				}, ctx => ctx.UseCustomQueueSuffix("special"));

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				await firstTsc.Task;
				await secondTsc.Task;

				/* Assert */
				Assert.True(true, "Should be delivered to both subscribers");
			}
		}

		[Fact]
		public async Task Should_Keep_Queue_Name_Intact_If_No_Custom_Prefix_Is_Used()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var firstClient = RawRabbitFactory.CreateTestClient())
			using (var secondClient = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseCustomQueueSuffix()
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
				}, ctx => ctx.UseCustomQueueSuffix(null));

				/* Test */
				await publisher.PublishAsync(new BasicMessage());
				Task.WaitAll(new Task[] {firstTsc.Task, secondTsc.Task}, TimeSpan.FromMilliseconds(300));

				/* Assert */
				var oneCompleted = firstTsc.Task.IsCompleted || secondTsc.Task.IsCompleted;
				var onlyOneCompleted = !(firstTsc.Task.IsCompleted && secondTsc.Task.IsCompleted);
				Assert.True(oneCompleted, "Should be delivered at least once");
				Assert.True(onlyOneCompleted, "Should not be delivered to both");
			}
		}

		[Fact]
		public async Task Should_Not_Interfere_With_Direct_RPC()
		{
			using (var responer = RawRabbitFactory.CreateTestClient())
			using (var requester = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p.UseApplicationQueueSuffix()
			}))
			{
				await responer.RespondAsync<BasicRequest, BasicResponse>(request => Task.FromResult(new BasicResponse()));
				var response = await requester.RequestAsync<BasicRequest, BasicResponse>();

				Assert.NotNull(response);
			}
		}
	}
}
