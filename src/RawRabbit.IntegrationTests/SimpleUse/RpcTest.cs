using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Consumer.Contract;
using RawRabbit.Consumer.Queueing;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class RpcTest : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Perform_Basic_Rpc_Without_Any_Config()
		{
			/* Setup */
			var response = new BasicResponse { Prop = "This is the reponse." };
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault();
			responder.RespondAsync<BasicRequest, BasicResponse>((req, i) =>
			{
				return Task.FromResult(response);
			});

			/* Test */
			var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>();

			/* Assert */
			Assert.Equal(recieved.Prop, response.Prop);
		}

		[Fact]
		public async Task Should_Perform_Rpc_Without_Direct_Reply_To()
		{
			/* Setup */
			var response = new BasicResponse { Prop = "This is the reponse." };
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault();
			responder.RespondAsync<BasicRequest, BasicResponse>((req, i) =>
			{
				return Task.FromResult(response);
			});

			/* Test */
			var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(),
				configuration: cfg => cfg
					.WithReplyQueue(
						q => q
							.WithName("special_reply_queue")
							.WithAutoDelete())
					.WithNoAck(false)
			);

			/* Assert */
			Assert.Equal(recieved.Prop, response.Prop);
		}

		[Fact]
		public async Task Should_Succeed_With_Multiple_Rpc_Calls_At_The_Same_Time()
		{
			/* Setup */
			var payloads = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
			var uniqueReponse = new ConcurrentStack<Guid>(payloads);
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault();
			responder.RespondAsync<BasicRequest, BasicResponse>((req, i) =>
			{
				Guid payload;
				if (!uniqueReponse.TryPop(out payload))
				{
					Assert.True(false, "No entities in stack. Try purgin the response queue.");
				};
				return Task.FromResult(new BasicResponse { Payload = payload });
			});

			/* Test */
			var first = requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest { Number = 1 });
			var second = requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest { Number = 2 });
			var third = requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest { Number = 3 });
			Task.WaitAll(first, second, third);

			/* Assert */
			Assert.Contains(first.Result.Payload, payloads);
			Assert.Contains(second.Result.Payload, payloads);
			Assert.Contains(third.Result.Payload, payloads);
			Assert.NotEqual(first.Result.Payload, second.Result.Payload);
			Assert.NotEqual(second.Result.Payload, third.Result.Payload);
			Assert.NotEqual(first.Result.Payload, third.Result.Payload);
		}

		[Fact]
		public async Task Should_Successfully_Perform_Nested_Requests()
		{
			/* Setup */
			var payload = Guid.NewGuid();

			var requester = BusClientFactory.CreateDefault();
			var firstResponder = BusClientFactory.CreateDefault();
			var secondResponder = BusClientFactory.CreateDefault();

			firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, i) =>
			{
				var secondResp = await firstResponder.RequestAsync<SecondRequest, SecondResponse>(new SecondRequest());
				return new FirstResponse { Infered = secondResp.Source };
			});
			secondResponder.RespondAsync<SecondRequest, SecondResponse>((req, i) =>
				Task.FromResult(new SecondResponse { Source = payload })
			);

			/* Test */
			var response = await requester.RequestAsync<FirstRequest, FirstResponse>(new FirstRequest());

			/* Assert */
			Assert.Equal(response.Infered, payload);
		}

		[Fact]
		public async Task Should_Work_With_Queueing_Consumer_Factory()
		{
			/* Setup */
			var response = new BasicResponse { Prop = "This is the reponse." };
			var requester = BusClientFactory.CreateDefault(
				new RawRabbitConfiguration { RequestTimeout = TimeSpan.FromHours(1) }
			);

			var responder = BusClientFactory.CreateDefault(service => service.AddTransient<IConsumerFactory, QueueingBaiscConsumerFactory>());
			responder.RespondAsync<BasicRequest, BasicResponse>((req, i) => Task.FromResult(response));

			/* Test */
			var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>();

			/* Assert */
			Assert.Equal(recieved.Prop, response.Prop);
		}

		[Fact]
		public async Task Should_Work_With_Different_Request_Types_For_Same_Responder()
		{
			/* Setup */
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault(service => service.AddTransient<IConsumerFactory, QueueingBaiscConsumerFactory>());
			
			responder.RespondAsync<FirstRequest, FirstResponse>((req, i) => Task.FromResult(new FirstResponse()));
			responder.RespondAsync<SecondRequest, SecondResponse>((req, i) => Task.FromResult(new SecondResponse()));

			/* Test */
			var firstResponse = await requester.RequestAsync<FirstRequest, FirstResponse>();
			var secondResponse = await requester.RequestAsync<SecondRequest, SecondResponse>();

			/* Assert */
			Assert.NotNull(firstResponse);
			Assert.NotNull(secondResponse);
		}

		[Fact]
		public async Task Should_Work_When_Not_Awaiting_One_Response_At_A_Time()
		{
			/* Setup */
			const int numberOfCalls = 10;
			var tasks = new Task[numberOfCalls];
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault();

			responder.RespondAsync<FirstRequest, FirstResponse>((req, i) => Task.FromResult(new FirstResponse { Infered = Guid.NewGuid()}));

			/* Test */
			for (int i = 0; i < numberOfCalls; i++)
			{
				var responseTask = requester.RequestAsync<FirstRequest, FirstResponse>();
				tasks[i] = responseTask;
			}
			Task.WaitAll(tasks);
			var ids = tasks
				.OfType<Task<FirstResponse>>()
				.Select(t => t.Result.Infered)
				.Distinct()
				.ToList();

			/* Assert */
			Assert.Equal(ids.Count, numberOfCalls);
		}
	}
}
