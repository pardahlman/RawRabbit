using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Client;
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
			var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

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
			var first = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest {Number = 1});
			var second = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest { Number = 2 });
			var third = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest { Number = 3 });

			/* Assert */
			Assert.Contains(first.Payload, payloads);
			Assert.Contains(second.Payload, payloads);
			Assert.Contains(third.Payload, payloads);
			Assert.NotEqual(first.Payload, second.Payload);
			Assert.NotEqual(second.Payload, third.Payload);
			Assert.NotEqual(first.Payload, third.Payload);
		}
	}
}
