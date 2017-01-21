using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Rpc
{
	public class RpcFundamentalTests
	{
		[Fact]
		public async Task Should_Return_Respose()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var sent = new BasicResponse
				{
					Prop = "I am the response"
				};
				await responder.RespondAsync<BasicRequest, BasicResponse>(request => Task.FromResult(sent)
				);

				/* Test */
				var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

				/* Assert */
				Assert.Equal(recieved.Prop, sent.Prop);
			}
		}
	}
}
