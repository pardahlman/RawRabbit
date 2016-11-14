#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System.Threading.Tasks;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Respond.Acknowledgement;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class AcknowledgementRespondTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Auto_Ack()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var sent = new BasicResponse
				{
					Prop = "I am the response"
				};
				await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
					sent
				);

				/* Test */
				var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

				/* Assert */
				Assert.Equal(recieved.Prop, sent.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Ack()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var sent = new BasicResponse
				{
					Prop = "I am the response"
				};
				await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
						new Ack<BasicResponse>(sent)
				);

				/* Test */
				var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

				/* Assert */
				Assert.Equal(recieved.Prop, sent.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Nack()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicRequest>();
				var secondTsc = new TaskCompletionSource<BasicRequest>();
				var sent = new BasicResponse {Prop = "I'm from the second handler"};

				await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
					{
						firstTsc.TrySetResult(request);
						return Respond.Nack<BasicResponse>();
					}
				);
				await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
				{
					secondTsc.TrySetResult(request);
					return Respond.Ack(sent);
				}
				);

				/* Test */
				var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());
				await firstTsc.Task;
				await secondTsc.Task;
				/* Assert */
				Assert.Equal(recieved.Prop, sent.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Return_Reject()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTsc = new TaskCompletionSource<BasicRequest>();
				var secondTsc = new TaskCompletionSource<BasicRequest>();
				var sent = new BasicResponse { Prop = "I'm from the second handler" };

				await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
				{
					firstTsc.TrySetResult(request);
					return Respond.Reject<BasicResponse>();
				}
				);
				await responder.RespondAsync<BasicRequest, BasicResponse>(async request =>
				{
					secondTsc.TrySetResult(request);
					return Respond.Ack(sent);
				}
				);

				/* Test */
				var recieved = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());
				await firstTsc.Task;
				await secondTsc.Task;
				/* Assert */
				Assert.Equal(recieved.Prop, sent.Prop);
			}
		}
	}
}
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
