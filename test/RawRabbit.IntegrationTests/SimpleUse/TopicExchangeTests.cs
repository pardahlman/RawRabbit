﻿using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;
using ExchangeType = RawRabbit.Configuration.Exchange.ExchangeType;

namespace RawRabbit.IntegrationTests.SimpleUse
{
    public class TopicExchangeTests : IntegrationTestBase
    {
        public TopicExchangeTests()
        {
            TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages");
            TestChannel.QueueDelete("basicmessage");
            TestChannel.QueueDelete("simplemessage");
        }

        public override void Dispose()
        {
            TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages");
            TestChannel.QueueDelete("basicmessage");
            TestChannel.QueueDelete("simplemessage");
            base.Dispose();
        }

        [Fact]
        public async Task Should_Deliver_Message_To_All_Subscribers_On_Exchange()
        {
            /* Setup */
            using (var publisher = TestClientFactory.CreateNormal())
            using (var firstSubscriber = TestClientFactory.CreateNormal())
            using (var secondSubscriber = TestClientFactory.CreateNormal())
            {
                var firstMsgTcs = new TaskCompletionSource<BasicMessage>();
                var secondMsgTcs = new TaskCompletionSource<BasicMessage>();

                firstSubscriber.SubscribeAsync<BasicMessage>((msg, i) =>
                {
                    firstMsgTcs.SetResult(msg);
                    return firstMsgTcs.Task;
                }, cfg => cfg
                    .WithQueue(q => q
                        .WithName("first.topic.queue"))
                    .WithRoutingKey("*.topic.queue")
                    .WithExchange(e => e.WithType(ExchangeType.Topic)));
                secondSubscriber.SubscribeAsync<BasicMessage>((msg, i) =>
                {
                    secondMsgTcs.SetResult(msg);
                    return firstMsgTcs.Task;
                }, cfg => cfg
                    .WithQueue(q => q
                        .WithName("second.topic.queue"))
                    .WithRoutingKey("*.topic.queue")
                    .WithExchange(e => e.AssumeInitialized()));

                /* Test */
                await publisher.PublishAsync(new BasicMessage(), configuration: cfg => cfg
                     .WithExchange(exchange => exchange.AssumeInitialized())
                     .WithRoutingKey("this.topic.queue"));
                Task.WaitAll(firstMsgTcs.Task, secondMsgTcs.Task);

                /* Assert */
                Assert.NotNull(firstMsgTcs.Task.Result);
                Assert.NotNull(secondMsgTcs.Task.Result);
            }
        }
    }
}
