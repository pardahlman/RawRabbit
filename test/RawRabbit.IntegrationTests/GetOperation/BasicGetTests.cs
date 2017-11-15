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
				var exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
				TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				var ackable = await client.GetAsync<BasicMessage>();

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Prop, message.Prop);
				TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));
				TestChannel.ExchangeDelete(exchangeName);
			}
		}
	}
}
