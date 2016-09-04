using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Attributes;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Attributes
{
	public class TopologyAttributeTests : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Work_For_Pub_Sub()
		{
			/* Setup */
			using (var client = BusClientFactory.CreateDefault(ioc => ioc
				.AddSingleton<IConfigurationEvaluator, AttributeConfigEvaluator>()
				))
			{
				var tcs = new TaskCompletionSource<AttributedMessage>();
				client.SubscribeAsync<AttributedMessage>((message, context) =>
				{
					tcs.TrySetResult(message);
					return Task.FromResult(true);
				}, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				await client.PublishAsync(new AttributedMessage());
				await tcs.Task;

				/* Assert */
				Assert.True(true, "Routing successful.");
			}
		}

		[Fact]
		public async Task Should_Work_For_Rpc()
		{
			/* Setup */
			using (var client = BusClientFactory.CreateDefault(ioc => ioc.AddSingleton<IConfigurationEvaluator, AttributeConfigEvaluator>()))
			{
				client.RespondAsync<AttributedRequest, AttributedResponse>((message, context) =>
					Task.FromResult(new AttributedResponse()),
					cfg => cfg.WithQueue(q => q.WithAutoDelete())
					);

				/* Test */
				await client.RequestAsync<AttributedRequest, AttributedResponse>();

				/* Assert */
				Assert.True(true, "Routing successful.");
			}
		}

		[Fact]
		public async Task Should_Honor_Custom_Config()
		{
			/* Setup */
			using (var client = BusClientFactory.CreateDefault(ioc => ioc
				.AddSingleton<IConfigurationEvaluator, AttributeConfigEvaluator>()
				))
			{
				var tcs = new TaskCompletionSource<AttributedMessage>();
				client.SubscribeAsync<AttributedMessage>((message, context) =>
				{
					tcs.TrySetResult(message);
					return Task.FromResult(true);
				}, c => c
					.WithRoutingKey("special")
					.WithExchange(e => e.WithName("special"))
					.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				await
					client.PublishAsync(new AttributedMessage(),
						configuration: c => c.WithRoutingKey("special").WithExchange(e => e.WithName("special")));
				await tcs.Task;

				/* Assert */
				Assert.True(true, "Routing successful.");
			}
		}

		[Queue(Name = "my_queue", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
		[Exchange(Name = "my_topic", Type = ExchangeType.Topic)]
		[Routing(RoutingKey = "my_key", NoAck = true, PrefetchCount = 50)]
		private class AttributedMessage { }

		[Queue(Name = "attributed_rpc", MessageTtl = 300, DeadLeterExchange = "dlx", Durable = false)]
		[Exchange(Name = "my_topic", Type = ExchangeType.Topic)]
		[Routing(RoutingKey = "my_key", NoAck = true, PrefetchCount = 50)]
		private class AttributedRequest { }

		private class AttributedResponse { }
	}
}
