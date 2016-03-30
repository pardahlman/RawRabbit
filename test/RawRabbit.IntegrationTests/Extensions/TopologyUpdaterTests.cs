using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.TopologyUpdater;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Extensions
{
	public class TopologyUpdaterTests : IntegrationTestBase
	{
		public TopologyUpdaterTests()
		{
			TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages");
		}

		public override void Dispose()
		{
			TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages");
			base.Dispose();
		}

		[Fact]
		public async Task Should_Update_Exhange_Type()
		{
			/* Setup */
			const string exchangeName = "topology";
			TestChannel.ExchangeDelete(exchangeName);
			TestChannel.ExchangeDeclare(exchangeName, RabbitMQ.Client.ExchangeType.Direct);

			var client = RawRabbitFactory.GetExtendableClient() as ExtendableBusClient<MessageContext>;

			/* Test */
			await client.UpdateTopologyAsync(t => t
				.ForExchange(exchangeName)
				.UseConfiguration(e => e
					.WithType(ExchangeType.Topic)
					.WithDurability(false))
			);

			/* Assert */
			TestChannel.ExchangeDeclare(exchangeName, RabbitMQ.Client.ExchangeType.Topic);
			Assert.True(true, "Did not throw");
			Assert.Throws<OperationInterruptedException>(() => TestChannel.ExchangeDeclare(exchangeName, RabbitMQ.Client.ExchangeType.Direct));
		}

		[Fact]
		public async Task Should_Not_Interupt_Existing_Subscribers_When_Using_Custom_Config()
		{
			/* Setup */
			var cfg = RawRabbitConfiguration.Local.AsLegacy();
			var client = RawRabbitFactory.GetExtendableClient(ioc => ioc.AddSingleton(s => cfg));
			var firstTcs = new TaskCompletionSource<BasicMessage>();
			var secondTcs = new TaskCompletionSource<BasicMessage>();
			client.SubscribeAsync<BasicMessage>((message, context) =>
			{
				if (!firstTcs.Task.IsCompleted)
				{
					firstTcs.SetResult(message);
					return firstTcs.Task;
				}
				if (!secondTcs.Task.IsCompleted)
				{
					secondTcs.SetResult(message);
					return secondTcs.Task;
				}
				return Task.FromResult(true);
			});
			
			/* Test */
			// 1. Verify subscriber
			client.PublishAsync<BasicMessage>();
			await firstTcs.Task;

			// 2. Change Type
			await client.UpdateTopologyAsync(c => c
				.ExchangeForMessage<BasicMessage>()
				.UseConfiguration(e => e.WithType(ExchangeType.Topic)));

			// 3. Verify subscriber
			client.PublishAsync<BasicMessage>();
			await secondTcs.Task;

			/* Assert */
			Assert.True(true, "First and second message was delivered.");
		}

		[Fact]
		public async Task Should_Not_Interupt_Existing_Subscribers_When_Using_Conventions()
		{
			/* Setup */
			var cfg = RawRabbitConfiguration.Local.AsLegacy();
			var client = RawRabbitFactory.GetExtendableClient(ioc => ioc.AddSingleton(s => cfg));
			var firstTcs = new TaskCompletionSource<BasicMessage>();
			var secondTcs = new TaskCompletionSource<BasicMessage>();
			client.SubscribeAsync<BasicMessage>((message, context) =>
			{
				if (!firstTcs.Task.IsCompleted)
				{
					firstTcs.SetResult(message);
					return firstTcs.Task;
				}
				if (!secondTcs.Task.IsCompleted)
				{
					secondTcs.SetResult(message);
					return secondTcs.Task;
				}
				return Task.FromResult(true);
			});

			/* Test */
			// 1. Verify subscriber
			client.PublishAsync<BasicMessage>();
			await firstTcs.Task;

			// 2. Change Type
			await client.UpdateTopologyAsync(c => c
				.UseConventionForExchange<BasicMessage>()
			);

			// 3. Verify subscriber
			client.PublishAsync<BasicMessage>();
			await secondTcs.Task;

			/* Assert */
			Assert.True(true, "First and second message was delivered.");
		}
	}
}
