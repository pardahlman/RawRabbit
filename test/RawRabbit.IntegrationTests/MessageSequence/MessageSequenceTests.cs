using System.Threading.Tasks;
using Microsoft.Extensions.Testing.Abstractions;
using RawRabbit.Context;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.MessageSequence;
using RawRabbit.Operations.Saga;
using RawRabbit.vNext.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.MessageSequence
{
	public class MessageSequenceTests
	{
		[Fact]
		public async Task Should_Create_Simple_Chain_Of_One_Send_And_Final_Recieve()
		{
			/* Setup */
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions
				{
					Plugins = p => p
						.PublishMessageContext<MessageContext>()
						.UseStateMachine()
						.UseMessageChaining()
				}))
			{
				await client.SubscribeAsync<BasicRequest, MessageContext>((request, context) =>
					client.PublishAsync(new BasicResponse(), context, cfg => cfg.WithRoutingKey($"{typeof(BasicResponse).Name}.{context.GlobalRequestId}"))
				);

				/* Test */
				var chain = client.ExecuteSequence<MessageContext, BasicResponse>(c => c
					.PublishAsync<BasicRequest>()
					.Complete<BasicResponse>()
				);

				await chain.Task;

				/* Assert */
				Assert.True(true, "Recieved Response");
			}
			
		}
	}
}
