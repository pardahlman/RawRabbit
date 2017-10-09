using System.Threading.Tasks;
using RawRabbit.Exceptions;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Compatibility
{
	public class InteroperationFunctionalityTests
	{
		[Fact]
		public async Task Throws_Publish_Confirm_Exception_If_Rpc_Response_Sent()
		{
			using (var client = RawRabbitFactory.CreateTestClient())
			{
				// Setup 
				await client.RespondAsync<BasicRequest, BasicResponse>(request => Task.FromResult(new BasicResponse()));
				await client.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

				try
				{
					// Test
					await client.PublishAsync(new BasicMessage());
				}
				catch (PublishConfirmException e)
				{
					// Assert
					Assert.True(false, e.Message);
				}
				Assert.True(true);
			}
		}
	}
}
