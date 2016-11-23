using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.TopologyUpdater;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;
using ExchangeType = RawRabbit.Configuration.Exchange.ExchangeType;

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
            try { TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages"); }
            catch (Exception) { }
            base.Dispose();
        }

        [Fact]
        public async Task Should_Update_Exhange_Type()
        {
            /* Setup */
            const string exchangeName = "topology";
            TestChannel.ExchangeDelete(exchangeName);
            TestChannel.ExchangeDeclare(exchangeName, RabbitMQ.Client.ExchangeType.Direct);

            using (var client = TestClientFactory.CreateExtendable())
            {
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
        }

        [Fact]
        public async Task Should_Not_Interupt_Existing_Subscribers_When_Using_Custom_Config()
        {
            /* Setup */
            var cfg = RawRabbitConfiguration.Local.AsLegacy();
            using (var client = TestClientFactory.CreateExtendable(ioc => ioc.AddSingleton(s => cfg)))
            {
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
        }

        [Fact]
        public async Task Should_Not_Interupt_Existing_Subscribers_When_Using_Conventions()
        {
            /* Setup */
            var cfg = RawRabbitConfiguration.Local.AsLegacy();
            using (var client = TestClientFactory.CreateExtendable(ioc => ioc.AddSingleton(s => cfg)))
            {
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

        [Fact]
        public async Task Should_Honor_Last_Configuration()
        {
            /* Setup */
            using (var client = TestClientFactory.CreateExtendable())
            {
                const string exchangeName = "topology";
                TestChannel.ExchangeDelete(exchangeName);

                /* Test */
                var result = await client.UpdateTopologyAsync(c => c
                    .ForExchange(exchangeName)
                    .UseConfiguration(e => e.WithType(ExchangeType.Headers))
                    .ForExchange(exchangeName)
                    .UseConfiguration(e => e.WithType(ExchangeType.Topic))
                    .ForExchange(exchangeName)
                    .UseConfiguration(e => e.WithType(ExchangeType.Direct))
                    .ForExchange(exchangeName)
                    .UseConfiguration(e => e.WithType(ExchangeType.Fanout)));

                /* Assert */
                Assert.Equal(result.Exchanges[0].Exchange.ExchangeType, ExchangeType.Fanout.ToString().ToLower());
            }
        }

        [Fact]
        public async Task Should_Use_Routing_Key_Transformer_If_Present()
        {
            /* Setup */
            using (var legacyClient = TestClientFactory.CreateExtendable(ioc => ioc.AddSingleton(s => RawRabbitConfiguration.Local.AsLegacy())))
            using (var currentClient = TestClientFactory.CreateExtendable())
            {
                var legacyTcs = new TaskCompletionSource<BasicMessage>();
                var currentTcs = new TaskCompletionSource<BasicMessage>();

                currentClient.SubscribeAsync<BasicMessage>((message, context) =>
                {
                    if (!currentTcs.Task.IsCompleted)
                    {
                        currentTcs.SetResult(message);
                        return currentTcs.Task;
                    }
                    if (!legacyTcs.Task.IsCompleted)
                    {
                        legacyTcs.SetResult(message);
                        return legacyTcs.Task;
                    }
                    return Task.FromResult(true);
                });

                /* Test */
                // 1. Verify subscriber
                currentClient.PublishAsync<BasicMessage>();
                await currentTcs.Task;

                // 2. Change Type
                await currentClient.UpdateTopologyAsync(c => c
                    .ExchangeForMessage<BasicMessage>()
                    .UseConfiguration(
                        exchange => exchange.WithType(ExchangeType.Direct),
                        bindingKey => bindingKey.Replace(".#", string.Empty))
                );

                // 3. Verify subscriber
                legacyClient.PublishAsync<BasicMessage>();
                await legacyTcs.Task;

                /* Assert */
                Assert.True(true);
            }
        }
    }
}
