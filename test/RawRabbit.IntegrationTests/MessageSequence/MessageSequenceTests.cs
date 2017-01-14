using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Testing.Abstractions;
using RawRabbit.Configuration;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.MessageSequence;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.StateMachine;
using RawRabbit.vNext.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.MessageSequence
{
	public class MessageSequenceTests
	{
		[Fact]
		public async Task Should_Create_Simple_Chain_Of_One_Send_And_Final_Recieve()
		{	
			/* Setup */
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseStateMachine()
					.UseContextForwaring()
			}))
			{
				await client.SubscribeAsync<BasicRequest, MessageContext>((request, context) =>
							client.PublishAsync(new BasicResponse())
				);

				/* Test */
				var chain = client.ExecuteSequence<MessageContext, BasicResponse>(c => c
						.PublishAsync<BasicRequest>()
						.Complete<BasicResponse>()
				);

				await chain.Task;

				/* Assert */
				Assert.True(true, "Recieved Response");
			}
		}

		[Fact]
		public async Task Should_Create_Chain_With_Publish_When_And_Complete()
		{
			/* Setup */
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseStateMachine()
					.UseContextForwaring()
			}))
			{
				var secondTcs = new TaskCompletionSource<SecondMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
							client.PublishAsync(new SecondMessage())
				);
				await client.SubscribeAsync<SecondMessage, MessageContext>((request, context) =>
							client.PublishAsync(new ThirdMessage())
				);

				/* Test */
				var chain = client.ExecuteSequence<MessageContext, ThirdMessage>(c => c
						.PublishAsync<FirstMessage>()
						.When<SecondMessage>((message, context) =>
						{
							secondTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
						.Complete<ThirdMessage>()
				);

				await chain.Task;
				await secondTcs.Task;

				/* Assert */
				Assert.NotNull(secondTcs.Task.Result);
				Assert.True(true, "Recieved Response");
			}
		}

		[Fact]
		public async Task Should_Abort_Execution_If_Configured_To()
		{
			/* Setup */
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseStateMachine()
					.UseContextForwaring()
			}))
			{
				var secondTcs = new TaskCompletionSource<SecondMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
							client.PublishAsync(new SecondMessage())
				);

				/* Test */
				var chain = client.ExecuteSequence<MessageContext, ThirdMessage>(c => c
						.PublishAsync<FirstMessage>()
						.When<SecondMessage>((message, context) =>
						{
							secondTcs.TrySetResult(message);
							return Task.FromResult(0);
						}, it => it.AbortsExecution())
						.Complete<ThirdMessage>()
				);

				await chain.Task;
				await secondTcs.Task;

				/* Assert */
				Assert.NotNull(secondTcs.Task.Result);
				Assert.True(chain.Aborted);
			}
		}

		[Fact]
		public async Task Should_Execute_Sequence_With_Multiple_Whens()
		{
			/* Setup */
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseStateMachine()
					.UseContextForwaring()
			}))
			{
				var secondTcs = new TaskCompletionSource<SecondMessage>();
				var thirdTsc = new TaskCompletionSource<ThirdMessage>();
				var forthTcs = new TaskCompletionSource<ForthMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
							client.PublishAsync(new SecondMessage())
				);
				await client.SubscribeAsync<SecondMessage, MessageContext>((request, context) =>
							client.PublishAsync(new ThirdMessage())
				);
				await client.SubscribeAsync<ThirdMessage, MessageContext>((request, context) =>
							client.PublishAsync(new ForthMessage())
				);
				await client.SubscribeAsync<ForthMessage, MessageContext>((request, context) =>
							client.PublishAsync(new FifthMessage())
				);

				/* Test */
				var chain = client.ExecuteSequence<MessageContext, FifthMessage>(c => c
						.PublishAsync<FirstMessage>()
						.When<SecondMessage>((message, context) =>
						{
							secondTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
						.When<ThirdMessage>((message, context) =>
						{
							thirdTsc.TrySetResult(message);
							return Task.FromResult(0);
						})
						.When<ForthMessage>((message, context) =>
						{
							forthTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
						.Complete<FifthMessage>()
				);

				await chain.Task;
				await secondTcs.Task;
				await thirdTsc.Task;
				await forthTcs.Task;

				/* Assert */
				Assert.NotNull(secondTcs.Task.Result);
				Assert.True(true, "Recieved Response");
			}
		}

		[Fact]
		public async Task Should_Work_For_Concurrent_Sequences()
		{
			/* Setup */
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseStateMachine()
					.UseContextForwaring()
			}))
			{
				var secondTcs = new TaskCompletionSource<SecondMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
							client.PublishAsync(new SecondMessage())
				);
				await client.SubscribeAsync<SecondMessage, MessageContext>((request, context) =>
							client.PublishAsync(new ThirdMessage())
				);

				const int numberOfSequences = 10;
				var sequences = new List<MessageSequence<ThirdMessage>>();
				for (var i = 0; i < numberOfSequences; i++)
				{
					var seq = client.ExecuteSequence<MessageContext, ThirdMessage>(c => c
							.PublishAsync<FirstMessage>()
							.When<SecondMessage>((message, context) =>
							{
								secondTcs.TrySetResult(message);
								return Task.FromResult(0);
							})
							.Complete<ThirdMessage>()
					);
					sequences.Add(seq);
				}
				Task.WaitAll(sequences.Select(s => s.Task).ToArray());

				/* Test */

				/* Assert */
				Assert.All(sequences, s => Assert.False(s.Aborted));
			}
		}

		[Fact]
		public async Task Should_Not_Invoke_Handler_If_Previous_Mandatory_Handler_Not_Invoked()
		{
			/* Setup */
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseStateMachine()
					.UseContextForwaring()
			}))
			{
				var firstTcs = new TaskCompletionSource<FirstMessage>();
				var secondTcs = new TaskCompletionSource<SecondMessage>();
				var thirdTcs = new TaskCompletionSource<ThirdMessage>();
				await client.SubscribeAsync<FirstMessage, MessageContext>((request, context) =>
				{
					firstTcs.TrySetResult(request);
					return client.PublishAsync(new ThirdMessage());
				});

				/* Test */
				client.ExecuteSequence<MessageContext, ForthMessage>(c => c
					.PublishAsync<FirstMessage>()
					.When<SecondMessage>((message, context) =>
						{
							secondTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
					.When<ThirdMessage>((message, context) =>
						{
							thirdTcs.TrySetResult(message);
							return Task.FromResult(0);
						})
					.Complete<ForthMessage>()
				);

				await firstTcs.Task;
				Task.WaitAll(new Task[] {secondTcs.Task, thirdTcs.Task}, TimeSpan.FromMilliseconds(400));
				secondTcs.Task.Wait(TimeSpan.FromMilliseconds(400));

				/* Assert */
				Assert.False(secondTcs.Task.IsCompleted);
				Assert.False(thirdTcs.Task.IsCompleted);
			}
		}

		[Fact]
		public async Task Should_Honor_Timeout()
		{
			/* Setup */
			var cfg = RawRabbitConfiguration.Local;
			cfg.RequestTimeout = TimeSpan.FromMilliseconds(200);
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
			{
				Plugins = p => p
					.UseMessageContext<MessageContext>()
					.UseStateMachine()
					.UseContextForwaring(),
				DependencyInjection = ioc => ioc.AddSingleton(cfg)
			}))
			{
				/* Test */
				var chain = client.ExecuteSequence<MessageContext, SecondMessage>(c => c
					.PublishAsync<FirstMessage>()
					.Complete<SecondMessage>()
				);

				/* Assert */
				await Assert.ThrowsAsync<TimeoutException>(async () => await chain.Task);
			}
		}
	}
}
