using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.GetOperation
{
	public class BasicGetTests : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Be_Able_To_Get_Message()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage {Prop = "Get me, get it?"};
				var conventions = new NamingConventions();
				TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), conventions.ExchangeNamingConvention(message.GetType()), conventions.RoutingKeyConvention(message.GetType()));

				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));

				/* Test */
				var ackable = await client.GetAsync<BasicMessage>();

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Prop, message.Prop);
			}
		}
	}
}
