using System;
using System.Threading.Tasks;
using Microsoft.Framework.DependencyInjection;
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
			var channelFactory = new ChannelFactory(new SingleNodeBroker(BrokerConfiguration.Local));
			var expectedResponse = new FirstResponse {Infered = Guid.NewGuid()};
			var reqeuster = BusClientFactory.CreateDefault(TimeSpan.FromHours(1));
			var responder = BusClientFactory.CreateDefault(s => s
					.AddInstance(typeof (IChannelFactory), channelFactory)
					.AddSingleton(p => new RawRabbitConfiguration { RequestTimeout = TimeSpan.FromHours(1) })
				);
			await responder.RespondAsync<FirstRequest, FirstResponse>((req, c) =>
			{
				channelFactory.CloseAll();
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
	}
}
