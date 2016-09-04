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

			var expectedId = Guid.NewGuid();
			var contextProvider = new MessageContextProvider<MessageContext>(new JsonSerializer(), () => new MessageContext { GlobalRequestId = expectedId });
			using (var subscriber = BusClientFactory.CreateDefault())
			using (var publisher = BusClientFactory.CreateDefault(collection => collection.AddSingleton<IMessageContextProvider<MessageContext>>(contextProvider)))
			{
				var subscribeTcs = new TaskCompletionSource<Guid>();

				subscriber.SubscribeAsync<BasicMessage>((msg, c) =>
				{
					subscribeTcs.SetResult(c.GlobalRequestId);
					return subscribeTcs.Task;
				}, c => c.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				publisher.PublishAsync<BasicMessage>();
				await subscribeTcs.Task;

				/* Assert */
				Assert.Equal(expected: expectedId, actual: subscribeTcs.Task.Result);
			}
		}

		[Fact]
		public async Task Should_Forward_Context_On_Publish()
		{
			/* Setup */
			using (var publisher = BusClientFactory.CreateDefault())
			using (var firstSubscriber = BusClientFactory.CreateDefault())
			using (var secondSubscriber = BusClientFactory.CreateDefault())
			{
				var firstCtxTcs = new TaskCompletionSource<MessageContext>();
				var secondCtxTcs = new TaskCompletionSource<MessageContext>();
				firstSubscriber.SubscribeAsync<BasicMessage>((msg, i) =>
				{
					firstCtxTcs.SetResult(i);
					firstSubscriber.PublishAsync(new SimpleMessage(), i.GlobalRequestId);
					return firstCtxTcs.Task;
				}, c => c.WithQueue(q => q.WithAutoDelete()));
				secondSubscriber.SubscribeAsync<SimpleMessage>((msg, i) =>
				{
					secondCtxTcs.SetResult(i);
					return secondCtxTcs.Task;
				}, c => c.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				publisher.PublishAsync<BasicMessage>();
				Task.WaitAll(firstCtxTcs.Task, secondCtxTcs.Task);

				/* Assert */
				Assert.Equal(firstCtxTcs.Task.Result.GlobalRequestId, secondCtxTcs.Task.Result.GlobalRequestId);
			}
		}

		[Fact]
		public async Task Should_Implicit_Forward_Context_On_Publish()
		{
			/* Setup */
			using (var publisher = BusClientFactory.CreateDefault())
			using (var firstSubscriber = BusClientFactory.CreateDefault())
			using (var secondSubscriber = BusClientFactory.CreateDefault())
			{
				var firstCtxTcs = new TaskCompletionSource<MessageContext>();
				var secondCtxTcs = new TaskCompletionSource<MessageContext>();

				firstSubscriber.SubscribeAsync<BasicMessage>((msg, i) =>
				{
					firstCtxTcs.SetResult(i);
					firstSubscriber.PublishAsync(new SimpleMessage());
					return firstCtxTcs.Task;
				}, c => c.WithQueue(q => q.WithAutoDelete()));
				secondSubscriber.SubscribeAsync<SimpleMessage>((msg, i) =>
				{
					secondCtxTcs.SetResult(i);
					return secondCtxTcs.Task;
				}, c => c.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				publisher.PublishAsync<BasicMessage>();
				Task.WaitAll(firstCtxTcs.Task, secondCtxTcs.Task);

				/* Assert */
				Assert.Equal(firstCtxTcs.Task.Result.GlobalRequestId, secondCtxTcs.Task.Result.GlobalRequestId);
			}
		}

		[Fact]
		public async Task Should_Forward_Context_On_Rpc()
		{
			/* Setup */
			using (var requester = BusClientFactory.CreateDefault())
			using (var firstResponder = BusClientFactory.CreateDefault())
			using (var secondResponder = BusClientFactory.CreateDefault())
			{
				var tcs = new TaskCompletionSource<bool>();
				MessageContext firstContext = null;
				MessageContext secondContext = null;

				firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, c) =>
				{
					firstContext = c;
					var resp = await firstResponder.RequestAsync<SecondRequest, SecondResponse>(new SecondRequest(), c.GlobalRequestId);
					return new FirstResponse { Infered = resp.Source };
				}, c => c.WithQueue(q => q.WithAutoDelete()));
				secondResponder.RespondAsync<SecondRequest, SecondResponse>((req, c) =>
				{
					secondContext = c;
					tcs.SetResult(true);
					return Task.FromResult(new SecondResponse { Source = Guid.NewGuid() });
				}, c => c.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				requester.RequestAsync<FirstRequest, FirstResponse>();
				await tcs.Task;

				/* Assert */
				Assert.Equal(firstContext.GlobalRequestId, secondContext.GlobalRequestId);
			}
			
		}

		[Fact]
		public async Task Should_Forward_Context_On_Rpc_To_Publish()
		{
			/* Setup */
			using (var requester = BusClientFactory.CreateDefault())
			using (var firstResponder = BusClientFactory.CreateDefault())
			using (var firstSubscriber = BusClientFactory.CreateDefault())
			{
				var tcs = new TaskCompletionSource<bool>();
				MessageContext firstContext = null;
				MessageContext secondContext = null;

				firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, c) =>
				{
					firstContext = c;
					await firstResponder.PublishAsync(new BasicMessage(), c.GlobalRequestId);
					return new FirstResponse();
				}, c => c.WithQueue(q => q.WithAutoDelete()));
				firstSubscriber.SubscribeAsync<BasicMessage>((req, c) =>
				{
					secondContext = c;
					tcs.SetResult(true);
					return tcs.Task;
				}, c => c.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				requester.RequestAsync<FirstRequest, FirstResponse>();
				await tcs.Task;

				/* Assert */
				Assert.Equal(firstContext.GlobalRequestId, secondContext.GlobalRequestId);
			}
		}
	}
}
