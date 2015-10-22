using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Conventions;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class MessageContextTests
	{
		[Fact]
		public async void Should_Send_Message_Context_Correctly()
		{
			/* Setup */
			var subscriber = BusClientFactory.CreateDefault();

			var expectedId = Guid.NewGuid();
			var subscribeTcs = new TaskCompletionSource<Guid>();
			var connection = new ConnectionFactory { HostName = "localhost" }.CreateConnection();
			var contextProvider = new DefaultMessageContextProvider(() => Task.FromResult(expectedId));
			var publisher = new BusClient(
				configEval: new ConfigurationEvaluator(new QueueConventions(), new ExchangeConventions()),
				subscriber: null,
				publisher: new Publisher<MessageContext>(new ChannelFactory(connection), new JsonMessageSerializer(), contextProvider),
				responder: null,
				requester: null
			);
			await subscriber.SubscribeAsync<BasicMessage>((msg, c) =>
			{
				subscribeTcs.SetResult(c.GlobalRequestId);
				return subscribeTcs.Task;
			});

			/* Test */
			publisher.PublishAsync<BasicMessage>();
			await subscribeTcs.Task;

			/* Assert */
			Assert.Equal(subscribeTcs.Task.Result, expectedId);
		}

		[Fact]
		public async void Should_Forward_Context_On_Publish()
		{
			/* Setup */
			var firstCtxTcs = new TaskCompletionSource<MessageContext>();
			var secondCtxTcs = new TaskCompletionSource<MessageContext>();
			var publisher = BusClientFactory.CreateDefault();
			var firstSubscriber = BusClientFactory.CreateDefault();
			var secondSubscriber = BusClientFactory.CreateDefault();
			await firstSubscriber.SubscribeAsync<BasicMessage>((msg, i) =>
			{
				firstCtxTcs.SetResult(i);
				firstSubscriber.PublishAsync(new SimpleMessage(), i.GlobalRequestId);
				return firstCtxTcs.Task;
			});
			await secondSubscriber.SubscribeAsync<SimpleMessage>((msg, i) =>
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
		public async void Should_Forward_Context_On_Rpc()
		{
			/* Setup */
			var tcs = new TaskCompletionSource<bool>();
			MessageContext firstContext = null;
			MessageContext secondContext = null;
			var requester = BusClientFactory.CreateDefault();
			var firstResponder = BusClientFactory.CreateDefault();
			var secondResponder = BusClientFactory.CreateDefault();

			await firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, c) =>
			{
				firstContext = c;
				var resp = await firstResponder.RequestAsync<SecondRequest, SecondResponse>(new SecondRequest(), c.GlobalRequestId);
				return new FirstResponse { Infered = resp.Source };
			});
			await secondResponder.RespondAsync<SecondRequest, SecondResponse>((req, c) =>
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
		public async void Should_Forward_Context_On_Rpc_To_Publish()
		{
			/* Setup */
			var tcs = new TaskCompletionSource<bool>();
			MessageContext firstContext = null;
			MessageContext secondContext = null;
			var requester = BusClientFactory.CreateDefault();
			var firstResponder = BusClientFactory.CreateDefault();
			var firstSubscriber = BusClientFactory.CreateDefault();

			await firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, c) =>
			{
				firstContext = c;
				await firstResponder.PublishAsync(new BasicMessage(), c.GlobalRequestId);
				return new FirstResponse();
			});
			await firstSubscriber.SubscribeAsync<BasicMessage>((req, c) =>
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
