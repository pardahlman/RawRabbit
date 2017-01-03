using System;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Exceptions;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Rpc
{
	public class ExceptionScenarioTests
	{
		[Fact]
		public async Task Should_Propegate_Responder_Exception_To_Requester()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				Func<BasicRequest, Task<BasicResponse>> errorHandler = request =>
				{
					throw new Exception("Kaboom");
				};
				await responder.RespondAsync(errorHandler);

				/* Test */
				/* Assert */
				await Assert.ThrowsAsync<MessageHandlerException>(
					async () => await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest())
				);
			}
		}

		[Fact]
		public async Task Should_Propegate_Responder_Exception_To_Requester_When_Responder_Has_Context()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */

				Func<BasicRequest, MessageContext, Task<BasicResponse>> handler = (request, context) =>
				{
					throw new Exception("Kaboom");
				};
				await responder.RespondAsync(handler);

				/* Test */
				/* Assert */
				await Assert.ThrowsAsync<MessageHandlerException>(
					async () => await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest())
				);
			}
		}
	}
}
