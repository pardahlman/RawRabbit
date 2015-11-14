using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Consumer.Contract;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class NackingTests : IntegrationTestBase
	{
		public NackingTests()
		{
			TestChannel.QueueDelete("basicmessage");
		}

		[Fact]
		public async void Should_Be_Able_To_Nack_Message()
		{
			/* Setup */
			var service = new ServiceCollection()
				.AddRawRabbit<AdvancedMessageContext>()
				.BuildServiceProvider();

			var firstResponder = service.GetService<IBusClient<AdvancedMessageContext>>();
			var secondResponder = service.GetService<IBusClient<AdvancedMessageContext>>();
			var requester = service.GetService<IBusClient<AdvancedMessageContext>>();

			var hasBeenNacked = false;
			firstResponder.RespondAsync<BasicRequest, BasicResponse>((request, context) =>
			{
				BasicResponse response = null;
				if (!hasBeenNacked)
				{
					context?.Nack();
					hasBeenNacked = true;
				}
				else
				{
					response = new BasicResponse();
				}
				return Task.FromResult(response);
			}, c => c.WithNoAck(false));
			secondResponder.RespondAsync<BasicRequest, BasicResponse>((request, context) =>
			{
				BasicResponse response = null;
				if (!hasBeenNacked)
				{
					context?.Nack();
					hasBeenNacked = true;
				}
				else
				{
					response = new BasicResponse();
				}
				return Task.FromResult(response);
			}, c => c.WithNoAck(false));

			/* Test */
			var result = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(), configuration: cfg => cfg
					 .WithReplyQueue(
						 q => q.WithName("special_reply_queue")));

			/* Assert */
			Assert.NotNull(result);
			Assert.True(hasBeenNacked);
		}

		[Fact]
		public async void Should_Be_Able_To_Nack_On_Subscribe()
		{
			/* Setup */
			var service = new ServiceCollection()
				.AddRawRabbit<AdvancedMessageContext>()
				.BuildServiceProvider();

			var subscriber = service.GetService<IBusClient<AdvancedMessageContext>>();
			var secondSubscriber = service.GetService<IBusClient<AdvancedMessageContext>>();
			var publisher = service.GetService<IBusClient<AdvancedMessageContext>>();
			var callcount = 0;
			var subscribeTcs = new TaskCompletionSource<bool>();
			var secondSubscribeTcs = new TaskCompletionSource<bool>();
			subscriber.SubscribeAsync<BasicMessage>((message, context) =>
			{
				Interlocked.Increment(ref callcount);
				context?.Nack();
				subscribeTcs.TrySetResult(true);
				return Task.FromResult(true);
			});
			secondSubscriber.SubscribeAsync<BasicMessage>((message, context) =>
			{
				secondSubscribeTcs.TrySetResult(true);
				return Task.FromResult(true);
			});

			Task.WaitAll(
				publisher.PublishAsync<BasicMessage>(),
				subscribeTcs.Task,
				secondSubscribeTcs.Task
			);

			TestChannel.QueueDelete("basicmessage");

			Assert.Equal(callcount, 1);
		}
	}
}
