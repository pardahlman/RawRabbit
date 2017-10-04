using System.Threading.Tasks;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Enrichers.Attributes;
using RawRabbit.Enrichers.Attributes.Middleware;
using RawRabbit.Instantiation;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class AttributeEnricherTests
	{
		[Fact]
		public async Task Should_Work_For_Publish()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() }))
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<AttributedMessage>();
				await subscriber.SubscribeAsync<AttributedMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(true);
				}, ctx => ctx
					.UseSubscribeConfiguration(cfg => cfg
						.Consume(c => c
							.WithRoutingKey("my_key"))
						.OnDeclaredExchange(e => e
							.WithName("my_topic")
							.WithType(ExchangeType.Topic))
				));

				/* Test */
				await publisher.PublishAsync(new AttributedMessage());
				await recievedTcs.Task;

				/* Assert */
				Assert.True(true, "Routing successful");
			}
		}

		[Fact]
		public async Task Should_Work_For_Subscribe()
		{
			using (var subscriber = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() }))
			using (var publisher = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<AttributedMessage>();
				await subscriber.SubscribeAsync<AttributedMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(true);
				});

				/* Test */
				await publisher.PublishAsync(new AttributedMessage(), ctx => ctx
						.UsePublishConfiguration(cfg => cfg
							.OnDeclaredExchange(e => e
								.WithName("my_topic")
								.WithType(ExchangeType.Topic))
							.WithRoutingKey("my_key")
				));
				await recievedTcs.Task;

				/* Assert */
				Assert.True(true, "Routing successful");
			}
		}

		[Fact]
		public async Task Should_Work_For_Request()
		{
			using (var requester = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
				{
					Plugins = plugin => plugin.UseAttributeRouting()
				}
			))
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<AttributedRequest>();
				await responder.RespondAsync<AttributedRequest, AttributedResponse>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(new AttributedResponse());
				}, ctx => ctx.UseRespondConfiguration(cfg => cfg
					.Consume(c => c
						.WithRoutingKey("my_request_key"))
					.OnDeclaredExchange(e => e
						.WithName("rpc_exchange")
						.WithType(ExchangeType.Topic))
				));

				/* Test */
				await requester.RequestAsync<AttributedRequest, AttributedResponse>(new AttributedRequest());
				await recievedTcs.Task;

				/* Assert */
				Assert.True(true, "Routing successful");
			}
		}

		[Fact]
		public async Task Should_Work_For_Responder()
		{
			using (var responder = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() }))
			using (var requester = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<AttributedRequest>();
				await responder.RespondAsync<AttributedRequest, AttributedResponse>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(new AttributedResponse());
				});

				/* Test */
				await requester.RequestAsync<AttributedRequest, AttributedResponse>(new AttributedRequest(), ctx => ctx
					.UseRequestConfiguration(cfg => cfg
						.PublishRequest(req => req
							.OnDeclaredExchange(e => e
								.WithName("rpc_exchange")
								.WithType(ExchangeType.Topic))
							.WithRoutingKey("my_request_key")
					)
				));
				await recievedTcs.Task;

				/* Assert */
				Assert.True(true, "Routing successful");
			}
		}

		[Fact]
		public async Task Should_Work_For_Full_Rpc()
		{
			using (var responder = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() }))
			using (var requester = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() }))
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<AttributedRequest>();
				await responder.RespondAsync<AttributedRequest, AttributedResponse>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(new AttributedResponse());
				});

				/* Test */
				await requester.RequestAsync<AttributedRequest, AttributedResponse>(new AttributedRequest());
				await recievedTcs.Task;

				/* Assert */
				Assert.True(true, "Routing successful");
			}
		}

		[Fact]
		public async Task Should_Work_For_Pub_Sub()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() }))
			using (var subscriber = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = plugin => plugin.UseAttributeRouting() }))
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<AttributedMessage>();
				await subscriber.SubscribeAsync<AttributedMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(new AttributedResponse());
				});

				/* Test */
				await publisher.PublishAsync(new AttributedMessage());
				await recievedTcs.Task;

				/* Assert */
				Assert.True(true, "Routing successful");
			}
		}

		[Queue(Name = "my_queue", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
		[Exchange(Name = "my_topic", Type = ExchangeType.Topic)]
		[Routing(RoutingKey = "my_key", AutoAck = true, PrefetchCount = 50)]
		private class AttributedMessage { }

		[Queue(Name = "attributed_request", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
		[Exchange(Name = "rpc_exchange", Type = ExchangeType.Topic)]
		[Routing(RoutingKey = "my_request_key", AutoAck = true, PrefetchCount = 50)]
		private class AttributedRequest { }

		[Queue(Name = "attributed_response", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
		[Exchange(Name = "rpc_exchange", Type = ExchangeType.Topic)]
		[Routing(RoutingKey = "my_response_key", AutoAck = true, PrefetchCount = 50)]
		private class AttributedResponse { }
	}
}
