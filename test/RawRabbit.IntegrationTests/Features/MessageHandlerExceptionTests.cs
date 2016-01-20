using System;
using System.Threading.Tasks;
using RawRabbit.Exceptions;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class MessageHandlerExceptionTests
	{
		[Fact]
		public async Task Should_Throw_Exception_To_Requester_If_Responder_Throws()
		{
			/* Setup */
			var responseException = new NotSupportedException("I'll throw this");
			var requester = BusClientFactory.CreateDefault(TimeSpan.FromHours(1));
			var responder = BusClientFactory.CreateDefault(TimeSpan.FromHours(1));
			responder.RespondAsync<BasicRequest, BasicResponse>((request, context) =>
			{
				throw responseException;
			});

			/* Test */
			/* Assert */
			var e = await Assert.ThrowsAsync<MessageHandlerException>(async () => await requester.RequestAsync<BasicRequest, BasicResponse>());
			Assert.Equal(e.InnerException.Message, responseException.Message);
		}
	}
}
