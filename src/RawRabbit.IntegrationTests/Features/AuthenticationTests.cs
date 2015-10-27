using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class AuthenticationTests
	{
		[Fact]
		public async void Should_Give_Clear_Error_Message_If_User_Does_Not_Exist()
		{
			/* Setup */
			var config = new RawRabbitConfiguration
			{
				Brokers =
				{
					new BrokerConfiguration
					{
						Username = "RawRabbit",
						Password = "wrong-pass",
						Hostname = "localhost",
						VirtualHost = "/"
					}
				}
			};

			/* Test */
			/* Assert */
			Assert.ThrowsAny<AuthenticationFailureException>(() => BusClientFactory.CreateDefault(config));
		}

		[Fact]
		public async void Should_Use_Guest_Credentials_By_Default()
		{
			/* Setup */
			var config = new RawRabbitConfiguration();

			/* Test */
			BusClientFactory.CreateDefault(config);

			/* Assert */
			Assert.True(true, "Does not throw.");
		}

		[Fact]
		public async void Should_Not_Throw_If_Credentials_Are_Correct()
		{
			/* Setup */
			var config = new RawRabbitConfiguration
			{
				Brokers =
				{
					new BrokerConfiguration
					{
						Username = "RawRabbit",
						Password = "RawRabbit",
						Hostname = "localhost",
						VirtualHost = "/"
					}
				}
			};

			/* Test */
			BusClientFactory.CreateDefault(config);

			/* Assert */
			Assert.True(true, "Does not throw.");
		}
	}
}
