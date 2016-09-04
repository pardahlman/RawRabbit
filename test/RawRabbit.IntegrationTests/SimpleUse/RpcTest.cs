using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
			using (var requester = BusClientFactory.CreateDefault())
			using (var responder = BusClientFactory.CreateDefault())
			{
				var response = new BasicResponse { Prop = "This is the response." };
				responder.RespondAsync<BasicRequest, BasicResponse>((req, i) =>
				{
					return Task.FromResult(response);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>();

				/* Assert */
				Assert.Equal(expected: response.Prop, actual: recieved.Prop);
			}
		}

		[Fact]
		public async Task Should_Perform_Basic_Rpc_For_Generic_Message_Types()
		{
			/* Setup */
			using (var requester = BusClientFactory.CreateDefault())
			using (var responder = BusClientFactory.CreateDefault())
			{
				var response = new GenericResponse<First, Second> { Prop = "This is the response." };
				responder.RespondAsync<GenericRequest<First, Second>, GenericResponse<First, Second>>((req, i) =>
				{
					return Task.FromResult(response);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				var recieved = await requester.RequestAsync<GenericRequest<First, Second>, GenericResponse<First, Second>>();

				/* Assert */
				Assert.Equal(expected: response.Prop, actual: recieved.Prop);
			}
		}

		[Fact]
		public async Task Should_Perform_Rpc_Without_Direct_Reply_To()
		{
			/* Setup */
			using (var requester = BusClientFactory.CreateDefault())
			using (var responder = BusClientFactory.CreateDefault())
			{
				var response = new BasicResponse { Prop = "This is the response." };
				responder.RespondAsync<BasicRequest, BasicResponse>((req, i) =>
				{
					return Task.FromResult(response);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				var firstRecieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(),
					configuration: cfg => cfg
						.WithReplyQueue(
							q => q
								.WithName("special_reply_queue")
								.WithAutoDelete())
						.WithNoAck(false));
				var secondRecieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(),
					configuration: cfg => cfg
						.WithReplyQueue(
							q => q
								.WithName("another_special_reply_queue")
								.WithAutoDelete())
						.WithNoAck(false)
				);

				/* Assert */
				Assert.Equal(expected: response.Prop, actual: firstRecieved.Prop);
				Assert.Equal(expected: response.Prop, actual: secondRecieved.Prop);
			}
		}

		[Fact]
		public async Task Should_Succeed_With_Multiple_Rpc_Calls_At_The_Same_Time()
		{
			/* Setup */
			using (var requester = BusClientFactory.CreateDefault())
			using (var responder = BusClientFactory.CreateDefault())
			{
				var payloads = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
				var uniqueResponse = new ConcurrentStack<Guid>(payloads);
				responder.RespondAsync<BasicRequest, BasicResponse>((req, i) =>
				{
					Guid payload;
					if (!uniqueResponse.TryPop(out payload))
					{
						Assert.True(false, "No entities in stack. Try purgin the response queue.");
					};
					return Task.FromResult(new BasicResponse { Payload = payload });
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

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
		}

		[Fact]
		public async Task Should_Successfully_Perform_Nested_Requests()
		{
			/* Setup */
			using (var requester = BusClientFactory.CreateDefault())
			using (var firstResponder = BusClientFactory.CreateDefault())
			using (var secondResponder = BusClientFactory.CreateDefault())
			{
				var payload = Guid.NewGuid();

				firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, i) =>
				{
					var secondResp = await firstResponder.RequestAsync<SecondRequest, SecondResponse>(new SecondRequest());
					return new FirstResponse { Infered = secondResp.Source };
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));
				secondResponder.RespondAsync<SecondRequest, SecondResponse>((req, i) =>
					Task.FromResult(new SecondResponse { Source = payload })
				, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				var response = await requester.RequestAsync<FirstRequest, FirstResponse>(new FirstRequest());

				/* Assert */
				Assert.Equal(expected: payload, actual: response.Infered);
			}
		}

		[Fact]
		public async Task Should_Work_When_Not_Awaiting_One_Response_At_A_Time()
		{
			/* Setup */
			using (var requester = BusClientFactory.CreateDefault())
			using (var responder = BusClientFactory.CreateDefault())
			{
				const int numberOfCalls = 10;
				var tasks = new Task[numberOfCalls];

				responder.RespondAsync<FirstRequest, FirstResponse>((req, i) =>
					Task.FromResult(new FirstResponse { Infered = Guid.NewGuid() }),
					cfg => cfg.WithQueue(q => q.WithAutoDelete()));

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
				Assert.Equal(expected: numberOfCalls, actual: ids.Count);
			}
		}
	}
}
