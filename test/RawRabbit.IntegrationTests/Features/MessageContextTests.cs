using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations;
using RawRabbit.Serialization;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class MessageContextTests
	{
		[Fact]
		public async Task Should_Send_Message_Context_Correctly()
		{
			/* Setup */
			var subscriber = BusClientFactory.CreateDefault();

			var expectedId = Guid.NewGuid();
			var subscribeTcs = new TaskCompletionSource<Guid>();
			var contextProvider = new MessageContextProvider<MessageContext>(new JsonSerializer(), () => new MessageContext {GlobalRequestId = expectedId});
			var publisher = BusClientFactory.CreateDefault(collection => collection.AddSingleton<IMessageContextProvider<MessageContext>>(contextProvider));
			subscriber.SubscribeAsync<BasicMessage>((msg, c) =>
			{
				subscribeTcs.SetResult(c.GlobalRequestId);
				return subscribeTcs.Task;
			});

			/* Test */
			publisher.PublishAsync<BasicMessage>();
			await subscribeTcs.Task;

			/* Assert */
			Assert.Equal(expected: expectedId, actual: subscribeTcs.Task.Result);
		}

		[Fact]
		public async Task Should_Forward_Context_On_Publish()
		{
			/* Setup */
			var firstCtxTcs = new TaskCompletionSource<MessageContext>();
			var secondCtxTcs = new TaskCompletionSource<MessageContext>();
			var publisher = BusClientFactory.CreateDefault();
			var firstSubscriber = BusClientFactory.CreateDefault();
			var secondSubscriber = BusClientFactory.CreateDefault();
			firstSubscriber.SubscribeAsync<BasicMessage>((msg, i) =>
			{
				firstCtxTcs.SetResult(i);
				firstSubscriber.PublishAsync(new SimpleMessage(), i.GlobalRequestId);
				return firstCtxTcs.Task;
			});
			secondSubscriber.SubscribeAsync<SimpleMessage>((msg, i) =>
			{
				secondCtxTcs.SetResult(i);
				return secondCtxTcs.Task;
			});

			/* Test */
			publisher.PublishAsync<BasicMessage>();
			Task.WaitAll(firstCtxTcs.Task, secondCtxTcs.Task);

			/* Assert */
			Assert.Equal(firstCtxTcs.Task.Result.GlobalRequestId, secondCtxTcs.Task.Result.GlobalRequestId);
		}

		[Fact]
		public async Task Should_Forward_Context_On_Rpc()
		{
			/* Setup */
			var tcs = new TaskCompletionSource<bool>();
			MessageContext firstContext = null;
			MessageContext secondContext = null;
			var requester = BusClientFactory.CreateDefault();
			var firstResponder = BusClientFactory.CreateDefault();
			var secondResponder = BusClientFactory.CreateDefault();

			firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, c) =>
			{
				firstContext = c;
				var resp = await firstResponder.RequestAsync<SecondRequest, SecondResponse>(new SecondRequest(), c.GlobalRequestId);
				return new FirstResponse { Infered = resp.Source };
			});
			secondResponder.RespondAsync<SecondRequest, SecondResponse>((req, c) =>
			{
				secondContext = c;
				tcs.SetResult(true);
				return Task.FromResult(new SecondResponse { Source = Guid.NewGuid() });
			});

			/* Test */
			requester.RequestAsync<FirstRequest, FirstResponse>();
			await tcs.Task;

			/* Assert */
			Assert.Equal(firstContext.GlobalRequestId, secondContext.GlobalRequestId);
		}

		[Fact]
		public async Task Should_Forward_Context_On_Rpc_To_Publish()
		{
			/* Setup */
			var tcs = new TaskCompletionSource<bool>();
			MessageContext firstContext = null;
			MessageContext secondContext = null;
			var requester = BusClientFactory.CreateDefault();
			var firstResponder = BusClientFactory.CreateDefault();
			var firstSubscriber = BusClientFactory.CreateDefault();

			firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, c) =>
			{
				firstContext = c;
				await firstResponder.PublishAsync(new BasicMessage(), c.GlobalRequestId);
				return new FirstResponse();
			});
			firstSubscriber.SubscribeAsync<BasicMessage>((req, c) =>
			{
				secondContext = c;
				tcs.SetResult(true);
				return tcs.Task;
			});

			/* Test */
			requester.RequestAsync<FirstRequest, FirstResponse>();
			await tcs.Task;

			/* Assert */
			Assert.Equal(firstContext.GlobalRequestId, secondContext.GlobalRequestId);
		}
	}
}
