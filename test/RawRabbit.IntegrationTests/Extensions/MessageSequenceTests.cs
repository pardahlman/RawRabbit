using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Extensions.CleanEverything;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.MessageSequence;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Extensions
{
	public class MessageSequenceTests : IntegrationTestBase
	{
		private readonly RawRabbit.Extensions.Disposable.ILegacyBusClient _client;

		public MessageSequenceTests()
		{
			_client = TestClientFactory.CreateExtendable();
		}

		public override void Dispose()
		{
			base.Dispose();
			_client.Dispose();
		}

		[Fact]
		public async Task Should_Create_Simple_Chain_Of_One_Send_And_Final_Recieve()
		{
			/* Setup */
			_client.SubscribeAsync<BasicRequest>((request, context) =>
				_client.PublishAsync(new BasicResponse(), context.GlobalRequestId)
			, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

			/* Test */
			var chain = _client.ExecuteSequence(c => c
				.PublishAsync<BasicRequest>()
				.Complete<BasicResponse>()
			);
			
			await chain.Task;

			/* Assert */
			Assert.True(true, "Recieed Response");
		}

		[Fact]
		public async Task Should_Create_Chain_With_Publish_When_And_Complete()
		{
			/* Setup */
			_client.SubscribeAsync<BasicRequest>(async (request, context) =>
			{
				await _client.PublishAsync(new BasicMessage(), context.GlobalRequestId);
				await _client.PublishAsync(new BasicResponse(), context.GlobalRequestId);
			}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

			/* Test */
			var chain = _client.ExecuteSequence(c => c
				.PublishAsync<BasicRequest>()
				.When<BasicMessage>((message, context) => Task.FromResult(true))
				.Complete<BasicResponse>()
			);
			await chain.Task;

			/* Assert */
			Assert.True(true, "Recieed Response");
		}

		[Fact(Skip = "Investigation needed")]
		public async Task Should_Call_Message_Handler_In_Correct_Order()
		{
			/* Setup */
			using (var publisher = TestClientFactory.CreateExtendable())
			{
				publisher.SubscribeAsync<FirstMessage>((message, context) =>
					publisher.PublishAsync(new SecondMessage(), context.GlobalRequestId));
				publisher.SubscribeAsync<SecondMessage>((message, context) =>
					publisher.PublishAsync(new ThirdMessage(), context.GlobalRequestId));
				publisher.SubscribeAsync<ThirdMessage>((message, context) =>
					publisher.PublishAsync(new ForthMessage(), context.GlobalRequestId));
				var recieveIndex = 0;
				var secondMsgDate = DateTime.MinValue;
				var thirdMsgDate = DateTime.MinValue;

				/* Test */
				var chain = _client.ExecuteSequence(c => c
					.PublishAsync(new FirstMessage())
					.When<SecondMessage>((message, context) =>
					{
						secondMsgDate = DateTime.Now;
						return Task.FromResult(true);
					})
					.When<ThirdMessage>((message, context) =>
					{
						thirdMsgDate = DateTime.Now;
						return Task.FromResult(true);
					})
					.Complete<ForthMessage>()
				);

				await chain.Task;

				/* Assert */
				Assert.True(secondMsgDate < thirdMsgDate);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Have_Multiple_Chains_Active()
		{
			/* Setup */
			var outer = 2;
			var inner = 4;

			_client.SubscribeAsync<BasicRequest>(async (request, context) =>
			{
				await _client.PublishAsync(new BasicMessage(), context.GlobalRequestId);
			});
			_client.SubscribeAsync<BasicMessage>(async (request, context) =>
			{
				await _client.PublishAsync(new BasicResponse(), context.GlobalRequestId);
			});

			/* Test */
			var triggerTasks = new Task[outer];
			var result = new ConcurrentBag<BasicResponse>();
			for (var o = 0; o < outer; o++)
			{
				triggerTasks[o] = new Task(() =>
				{
					for (int i = 0; i < inner; i++)
					{
						var chain = _client.ExecuteSequence(c => c
							.PublishAsync<BasicRequest>()
							.When<BasicMessage>((message, context) => Task.FromResult(true))
							.Complete<BasicResponse>()
							);
						result.Add(chain.Task.Result);
					}
				});
			}
			foreach (var triggerTask in triggerTasks)
			{
				triggerTask.Start();
			}
			
			Task.WaitAll(triggerTasks);

			/* Assert */
			Assert.Equal(inner*outer, result.Count);
		}

		[Fact]
		public async Task Should_Honor_Abort_Exeuction()
		{
			/* Setup */
			var thirdHandlerCalled = false;
			_client.SubscribeAsync<FirstMessage>(async (request, context) =>
			{
				await _client.PublishAsync(new SecondMessage(), context.GlobalRequestId);
			});
			_client.SubscribeAsync<SecondMessage>(async (request, context) =>
			{
				await _client.PublishAsync(new ThirdMessage(), context.GlobalRequestId);
			});

			/* Test */
			var chain = _client.ExecuteSequence(c => c
				.PublishAsync<FirstMessage>()
				.When<SecondMessage>(
					(message, context) => Task.FromResult(true),
					(option) => option.AbortsExecution())
				.When<ThirdMessage>(
					(message, context) =>
					{
						thirdHandlerCalled = true;
						return Task.FromResult(true);
					})
				.Complete<ForthMessage>()
			);
			await chain.Task;

			/* Assert */
			Assert.True(chain.Aborted, "Execution should be aborted");
			Assert.False(thirdHandlerCalled, "Handler should not be called");
		}

		[Fact]
		public async Task Should_Skip_Optional_Handler_If_Not_Matched()
		{
			/* Setup */
			var secondHandlerCalled = false;
			_client.SubscribeAsync<FirstMessage>(async (request, context) =>
			{
				await _client.PublishAsync(new ThirdMessage(), context.GlobalRequestId);
			});
			_client.SubscribeAsync<ThirdMessage>(async (request, context) =>
			{
				await _client.PublishAsync(new ForthMessage(), context.GlobalRequestId);
			});

			/* Test */
			var chain = _client.ExecuteSequence(c => c
				.PublishAsync<FirstMessage>()
				.When<SecondMessage>(
					(message, context) =>
					{
						secondHandlerCalled = true;
						return Task.FromResult(true);
					},
					(option) => option.IsOptional())
				.When<ThirdMessage>(
					(message, context) => Task.FromResult(true))
				.Complete<ForthMessage>()
			);
			await chain.Task;

			/* Assert */
			Assert.Equal(1, chain.Skipped.Count);
			Assert.False(secondHandlerCalled, "Handler should not be called");
		}

		[Fact]
		public async Task Should_Call_Matching_Optional_Handler()
		{
			/* Setup */
			var secondHandlerCalled = false;
			_client.SubscribeAsync<FirstMessage>(async (request, context) =>
			{
				await _client.PublishAsync(new SecondMessage(), context.GlobalRequestId);
			});
			_client.SubscribeAsync<SecondMessage>(async (request, context) =>
			{
				await _client.PublishAsync(new ThirdMessage(), context.GlobalRequestId);
			});
			_client.SubscribeAsync<ThirdMessage>(async (request, context) =>
			{
				await _client.PublishAsync(new ForthMessage(), context.GlobalRequestId);
			});

			/* Test */
			var chain = _client.ExecuteSequence(c => c
				.PublishAsync<FirstMessage>()
				.When<SecondMessage>(
					(message, context) =>
					{
						secondHandlerCalled = true;
						return Task.FromResult(true);
					},
					(option) => option.IsOptional())
				.When<ThirdMessage>(
					(message, context) => Task.FromResult(true))
				.Complete<ForthMessage>()
			);
			await chain.Task;

			/* Assert */
			Assert.Equal(0, chain.Skipped.Count);
			Assert.True(secondHandlerCalled, "Handler should be called");
		}

		[Fact]
		public async Task Should_Honor_Timeout()
		{
			/* Setup */
			var cfg = RawRabbitConfiguration.Local;
			cfg.RequestTimeout = TimeSpan.FromMilliseconds(200);
			using (var client = TestClientFactory.CreateExtendable(ioc => ioc.AddSingleton(c => cfg)))
			{
				/* Test */
				var chain = client.ExecuteSequence(c => c
					.PublishAsync<FirstMessage>()
					.Complete<SecondMessage>()
				);

				/* Assert */
				await Assert.ThrowsAsync<TimeoutException>(async () => await chain.Task);
			}
		}
	}
}
