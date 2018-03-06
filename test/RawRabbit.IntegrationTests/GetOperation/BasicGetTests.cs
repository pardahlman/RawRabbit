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

		[Fact]
		public async Task Should_Be_Able_To_Get_BasicGetResult_Message()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage { Prop = "Get me, get it?" };
				var conventions = new NamingConventions();
				var exchangeName = conventions.ExchangeNamingConvention(message.GetType());
				var queueName = conventions.QueueNamingConvention(message.GetType());
				TestChannel.QueueDeclare(queueName, true, false, false, null);
				TestChannel.ExchangeDeclare(exchangeName, ExchangeType.Topic);
				TestChannel.QueueBind(queueName, exchangeName, conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, ctx => ctx.UsePublishConfiguration(cfg => cfg.OnExchange(exchangeName)));

				/* Test */
				var ackable = await client.GetAsync(cfg => cfg.FromQueue(queueName));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.NotEmpty(ackable.Content.Body);
				TestChannel.QueueDelete(queueName);
				TestChannel.ExchangeDelete(exchangeName);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_BasicGetResult_When_Queue_IsEmpty()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage();
				var conventions = new NamingConventions();
				var queueName = conventions.QueueNamingConvention(message.GetType());
				TestChannel.QueueDeclare(queueName, true, false, false, null);

				/* Test */
				var ackable = await client.GetAsync(cfg => cfg.FromQueue(queueName));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Null(ackable.Content);
				TestChannel.QueueDelete(queueName);
			}
		}
	}
}
