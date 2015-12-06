using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.BulkGet;
using RawRabbit.Extensions.Client;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Extensions
{
	public class BulkGetTests
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

			using (var subscriber = BusClientFactory.CreateDefault())
			{
				subscriber.SubscribeAsync<BasicMessage>((message, context) => Task.FromResult(true), cfg => cfg.WithSubscriberId(firstSuffix));
				subscriber.SubscribeAsync<BasicMessage>((message, context) => Task.FromResult(true), cfg => cfg.WithSubscriberId(secondSuffix));
				subscriber.SubscribeAsync<SimpleMessage>((message, context) => Task.FromResult(true), cfg => cfg.WithSubscriberId(firstSuffix));
			}
		}

		[Fact]
		public void Should_Be_Able_To_Bulk_Get_Messages()
		{
			var firstBasicMsg = new BasicMessage { Prop = "This is the first message" };
			var secondBasicMsg = new BasicMessage { Prop = "This is the second message" };
			var thridBasicMsg = new BasicMessage { Prop = "This is the thrid message" };
			var firstSimpleMsg = new SimpleMessage { IsSimple = true };

			var client = RawRabbitFactory.GetExtendableClient() as ExtendableBusClient<MessageContext>;
			client.PublishAsync(secondBasicMsg);
			client.PublishAsync(firstBasicMsg);
			client.PublishAsync(thridBasicMsg);
			client.PublishAsync(firstSimpleMsg);

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
			
			Assert.Equal(basics.Count, 4);
			Assert.Equal(simple.Count, 1);
		}
	}
}
