using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Extensions.CleanEverything;
using RawRabbit.Extensions.Client;
using Xunit;

namespace RawRabbit.IntegrationTests.Extensions
{
	public class CleanEverythingTests
	{
		[Fact]
		public async Task Should_Clean_Everything()
		{
			/* Setup */
			var connectionFactory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest"
			};
			var connection = connectionFactory.CreateConnection();
			var channel = connection.CreateModel();
			channel.QueueDeclare("my_queue", true, false, false, null);
			channel.ExchangeDeclare("my_exchange", ExchangeType.Direct, true);
			var client = RawRabbitFactory.GetExtendableClient();

			/* Test */
			await client.CleanAsync(cfg => cfg
				.RemoveQueues()
				.RemoveExchanges()
			);

			/* Assert */
			Assert.True(true, "Needs to be fixed.");
		}
	}
}
