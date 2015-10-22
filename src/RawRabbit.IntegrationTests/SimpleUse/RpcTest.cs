using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class RpcTest : IntegrationTestBase
	{
		[Fact]
		public async void Should_Perform_Basic_Rpc_Without_Any_Config()
		{
			/* Setup */
			var response = new BasicResponse {Prop = "This is the reponse."};
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault();
			await responder.RespondAsync<BasicRequest, BasicResponse>((req, i) => Task.FromResult(response));
			
			/* Test */
			var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>();

			/* Assert */
			Assert.Equal(recieved.Prop, response.Prop);
		}

		[Fact]
		public async void Should_Succeed_With_Multiple_Rpc_Calls_At_The_Same_Time()
		{
			/* Setup */
			var payloads = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
			var uniqueReponse = new ConcurrentStack<Guid>(payloads);
			var requester = BusClientFactory.CreateDefault();
			var responder = BusClientFactory.CreateDefault();
			await responder.RespondAsync<BasicRequest, BasicResponse>((req, i) =>
			{
				Guid payload;
				if (!uniqueReponse.TryPop(out payload))
				{
					Assert.True(false, "No entities in stack. Try purgin the response queue.");
				};
				return Task.FromResult(new BasicResponse {Payload = payload});
			});

			/* Test */
			var first = requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest {Number = 1});
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
		public async void Should_Successfully_Perform_Nested_Requests()
		{
			/* Setup */
			var payload = Guid.NewGuid();

			var requester = BusClientFactory.CreateDefault();
			var firstResponder = BusClientFactory.CreateDefault();
			var secondResponder = BusClientFactory.CreateDefault();

			await firstResponder.RespondAsync<FirstRequest, FirstResponse>(async (req, i) =>
			{
				var secondResp = await firstResponder.RequestAsync<SecondRequest, SecondResponse>(new SecondRequest());
				return new FirstResponse {Infered = secondResp.Source};
			});
			await secondResponder.RespondAsync<SecondRequest, SecondResponse>((req, i) =>
				Task.FromResult(new SecondResponse {Source = payload})
			);

			/* Test */
			var response = await requester.RequestAsync<FirstRequest, FirstResponse>(new FirstRequest());
			
			/* Assert */
			Assert.Equal(response.Infered, payload);
		}
	}
}
