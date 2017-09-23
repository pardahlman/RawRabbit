using System;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Request.Middleware;
using RawRabbit.Operations.Respond.Core;
using Xunit;

namespace RawRabbit.IntegrationTests.Rpc
{
	public class RpcFundamentalTests
	{
		[Fact]
		public async Task Should_Return_Respose_Without_Any_Additional_Configuration()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var sent = new BasicResponse
				{
					Prop = "I am the response"
				};
				await responder.RespondAsync<BasicRequest, BasicResponse>(request => Task.FromResult(sent)
				);

				/* Test */
				var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

				/* Assert */
				Assert.Equal(recieved.Prop, sent.Prop);
			}
		}

		[Fact]
		public async Task Should_Return_Response_When_Using_Custom_Request_And_Response_Configuration()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var sent = new BasicResponse { Prop = "I am the response" };
				await responder.RespondAsync<BasicRequest, BasicResponse>(message =>
						Task.FromResult(sent),
					ctx => ctx.UseRespondConfiguration(cfg => cfg
						.Consume(c => c
							.WithRoutingKey("custom_key"))
						.FromDeclaredQueue(q => q
							.WithName("custom_queue")
							.WithAutoDelete())
						.OnDeclaredExchange(e => e
							.WithName("custom_exchange")
							.WithAutoDelete()))
				);

				/* Test */
				var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(), ctx => ctx
					.UseRequestConfiguration(cfg => cfg
						.PublishRequest(p => p
							.OnDeclaredExchange(e => e
								.WithName("custom_exchange")
								.WithAutoDelete())
							.WithRoutingKey("custom_key")
							.WithProperties(prop => prop.DeliveryMode = 1))
						.ConsumeResponse(r => r
							.Consume(c => c
								.WithRoutingKey("response_key"))
							.FromDeclaredQueue(q => q
								.WithName("response_queue")
								.WithAutoDelete())
							.OnDeclaredExchange(e => e
								.WithName("response_exchange")
								.WithAutoDelete()
							)
						)
					));

				/* Assert */
				Assert.Equal(recieved.Prop, sent.Prop);
			}
		}


		[Fact]
		public async Task Should_Work_With_Dedicated_Consumer_And_Custom_Response_Queue()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				await responder.RespondAsync<BasicRequest, BasicResponse>(async request => new BasicResponse());
				var numberOfRequests = 10;
				var responses = new Task<BasicResponse>[numberOfRequests];

				/* Test */
				for (var i = 0; i < numberOfRequests; i++)
				{
					responses[i] = requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest(), ctx => ctx
						.UseDedicatedResponseConsumer()
						.UseRequestConfiguration(cfg => cfg
							.ConsumeResponse(r => r
								.Consume(c => c
									.WithRoutingKey($"response_key_{Guid.NewGuid()}"))
								.FromDeclaredQueue(q => q
									.WithName($"response_queue_{Guid.NewGuid()}")
									.WithAutoDelete())
								.OnDeclaredExchange(e => e
									.WithName("response_exchange")
									.WithAutoDelete(false)
								)
							)
						)
					);
				}
				Task.WaitAll(responses);
				await requester.DeleteExchangeAsync("response_exchange");
				/* Assert */
				Assert.True(responses.All(r => r.IsCompleted));
			}
		}
	}
}
