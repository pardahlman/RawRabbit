using System;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class LostConnectionTests : IntegrationTestBase
	{
		public LostConnectionTests()
		{
			TestChannel.QueueDelete("firstresponse");
		}

		[Fact]
		public async void Should_Succeed_To_Send_Response_Even_If_Channel_Is_Closed()
		{
			/* Setup */
			var channelFactory = new ConfigChannelFactory(new ConnectionFactory {HostName = "localhost"});
			var channel = channelFactory.GetChannel();
			var expectedResponse = new FirstResponse {Infered = Guid.NewGuid()};
			var reqeuster = BusClientFactory.CreateDefault(TimeSpan.FromHours(1));
			var responder = BusClientFactory.CreateDefault(new RawRabbitConfiguration {RequestTimeout = TimeSpan.FromHours(1)}, s => s.AddInstance(typeof (IChannelFactory), channelFactory));
			await responder.RespondAsync<FirstRequest, FirstResponse>((req, c) =>
			{
				channelFactory.CloseAll();
				//channel.Close();
				//while (channel.IsOpen)
				//{
				//	// wait for channel to close
				//}
				return Task.FromResult(expectedResponse);
			});

			/* Test */
			var actualResponse = await reqeuster.RequestAsync<FirstRequest, FirstResponse>();
			
			uint msgCount = 1;
			do
			{
				await Task.Delay(50);
				var queue = TestChannel.QueueDeclare("firstresponse", false, false, false, null);
				msgCount = queue.MessageCount;

			} while (msgCount != 0);
			/* Assert */
			Assert.Equal(actualResponse.Infered, expectedResponse.Infered);
		}

		[Fact]
		public async void Should_Do_Stuff_On_Lost_Connection()
		{
			/* Setup */
			var connectionFactory = new ConnectionFactory {HostName = "localhost"};
			var connection = connectionFactory.CreateConnection();
			
			var mockedFactory = new Mock<IConnectionFactory>();
			mockedFactory
				.Setup(f => f.CreateConnection())
				.Returns(connection);
			
			var publisher = BusClientFactory.CreateDefault();
			var subscriber = BusClientFactory.CreateDefault(null, s => s.AddInstance(typeof(IConnectionFactory), mockedFactory.Object));

			await subscriber.SubscribeAsync<BasicMessage>((message, context) =>
			{
				connection.Close();
				return Task.FromResult(true);
			});

			/* Test */
			await publisher.PublishAsync<BasicMessage>();

			/* Assert */
			Assert.True(true);
		}
	}
}
