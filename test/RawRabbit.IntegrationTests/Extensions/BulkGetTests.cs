using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Context;
using RawRabbit.Extensions.BulkGet;
using RawRabbit.Extensions.Client;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Logging;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Extensions
{
    public class BulkGetTests : IntegrationTestBase
    {
        private readonly string _firstBasicQueue;
        private readonly string _secondBasicQueue;
        private readonly string _firstSimpleQueue;

        public BulkGetTests()
        {
            var basicQueueBase = "basicmessage";
            var simpleQueueBase = "simplemessage";
            var firstSuffix = "first_subscriber";
            var secondSuffix = "second_subscriber";
            _firstBasicQueue = $"{basicQueueBase}_{firstSuffix}";
            _secondBasicQueue = $"{basicQueueBase}_{secondSuffix}";
            _firstSimpleQueue = $"{simpleQueueBase}_{firstSuffix}";

            using (var subscriber = TestClientFactory.CreateNormal())
            {
                subscriber.SubscribeAsync<BasicMessage>((message, context) => Task.FromResult(true), cfg => cfg.WithSubscriberId(firstSuffix).WithQueue(q => q.WithAutoDelete(false)));
                subscriber.SubscribeAsync<BasicMessage>((message, context) => Task.FromResult(true), cfg => cfg.WithSubscriberId(secondSuffix).WithQueue(q => q.WithAutoDelete(false)));
                subscriber.SubscribeAsync<SimpleMessage>((message, context) => Task.FromResult(true), cfg => cfg.WithSubscriberId(firstSuffix).WithQueue(q => q.WithAutoDelete(false)));
            }
        }

        public override void Dispose()
        {
            TestChannel.QueueDelete(_firstBasicQueue);
            TestChannel.QueueDelete(_firstSimpleQueue);
            TestChannel.QueueDelete(_secondBasicQueue);
            base.Dispose();
        }

        [Fact]
        public async Task Should_Be_Able_To_Bulk_Get_Messages()
        {
            var firstBasicMsg = new BasicMessage { Prop = "This is the first message" };
            var secondBasicMsg = new BasicMessage { Prop = "This is the second message" };
            var thridBasicMsg = new BasicMessage { Prop = "This is the thrid message" };
            var firstSimpleMsg = new SimpleMessage { IsSimple = true };

            using (var client = TestClientFactory.CreateExtendable())
            {
                await client.PublishAsync(secondBasicMsg);
                await client.PublishAsync(firstBasicMsg);
                await client.PublishAsync(thridBasicMsg);
                await client.PublishAsync(firstSimpleMsg);
                await Task.Delay(500);

                var bulk = client.GetMessages(cfg => cfg
                    .ForMessage<BasicMessage>(msg => msg
                        .FromQueues(_firstBasicQueue, _secondBasicQueue)
                        .WithBatchSize(4))
                    .ForMessage<SimpleMessage>(msg => msg
                        .FromQueues(_firstSimpleQueue)
                        .GetAll()
                        .WithNoAck()
                    ));
                var basics = bulk.GetMessages<BasicMessage>().ToList();
                var simple = bulk.GetMessages<SimpleMessage>().ToList();
                bulk.AckAll();

                Assert.Equal(expected: 4, actual: basics.Count);
                Assert.Equal(expected: 1, actual: simple.Count);
            }
        }
    }
}
