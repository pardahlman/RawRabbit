using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.GetOperation
{
	public class GetManyTests : IntegrationTestBase
	{
		[Fact]
		public async Task Should_Be_Able_To_Get_Message_When_Batch_Size_And_Queue_Length_Are_Equal()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage { Prop = "Get me, get it?" };
				var conventions = new NamingConventions();
				TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), conventions.ExchangeNamingConvention(message.GetType()), conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));

				/* Test */
				var ackable = await client.GetManyAsync<BasicMessage>(3);
				TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Count, 3);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_Message_When_Batch_Size_Is_Larger_Than_Queue_Length()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage { Prop = "Get me, get it?" };
				var conventions = new NamingConventions();
				TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), conventions.ExchangeNamingConvention(message.GetType()), conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));

				/* Test */
				var ackable = await client.GetManyAsync<BasicMessage>(10);
				TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Count, 3);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Get_Message_When_Batch_Size_Is_Smaller_Than_Queue_Length()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage { Prop = "Get me, get it?" };
				var conventions = new NamingConventions();
				TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), conventions.ExchangeNamingConvention(message.GetType()), conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));

				/* Test */
				var ackable = await client.GetManyAsync<BasicMessage>(2);
				TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));

				/* Assert */
				Assert.NotNull(ackable);
				Assert.Equal(ackable.Content.Count, 2);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Nack_One_In_Batch()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage { Prop = "Get me, get it?" };
				var nacked = new BasicMessage { Prop = "Not me! Plz?" };
				var conventions = new NamingConventions();
				TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), conventions.ExchangeNamingConvention(message.GetType()), conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(nacked, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));

				/* Test */
				var ackableList = await client.GetManyAsync<BasicMessage>(3);
				foreach (var ackableMsg in ackableList.Content)
				{
					if (string.Equals(ackableMsg.Content.Prop, message.Prop))
					{
						ackableMsg.Ack();
					}
					else
					{
						ackableMsg.Nack();
					}
				}
				var getAgain = await client.GetAsync<BasicMessage>();
				TestChannel.QueueDelete(conventions.QueueNamingConvention(message.GetType()));

				/* Assert */
				Assert.NotNull(getAgain);
				Assert.Equal(getAgain.Content.Prop, nacked.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Ack_Messages_And_Then_Full_List()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage { Prop = "Get me, get it?" };
				var conventions = new NamingConventions();
				TestChannel.QueueDeclare(conventions.QueueNamingConvention(message.GetType()), true, false, false, null);
				TestChannel.ExchangeDeclare(conventions.ExchangeNamingConvention(message.GetType()), ExchangeType.Topic);
				TestChannel.QueueBind(conventions.QueueNamingConvention(message.GetType()), conventions.ExchangeNamingConvention(message.GetType()), conventions.RoutingKeyConvention(message.GetType()) + ".#");

				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));
				await client.PublishAsync(message, cfg => cfg.OnDeclaredExchange(e => e.AssumeInitialized()));

				/* Test */
				var ackable = await client.GetManyAsync<BasicMessage>(3);
				ackable.Content[1].Ack();
				ackable.Ack();

				/* Assert */
				Assert.True(true);
			}
		}
	}
}
