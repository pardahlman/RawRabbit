using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Conventions;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations;
using RawRabbit.Serialization;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class MessageContextTests
	{
		[Fact]
		public async void Should_Send_Message_Context_Correctly()
		{
			/* Setup */
			var subscriber = BusClientFactory.CreateDefault();

			var expectedId = Guid.NewGuid();
			var subscribeTcs = new TaskCompletionSource<Guid>();
			var connection = new ConnectionFactory { HostName = "localhost"}.CreateConnection();
			var contextProvider = new DefaultMessageContextProvider(() => Task.FromResult(expectedId));
			var publisher =  new BusClient(
				configEval: new ConfigurationEvaluator(new QueueConventions(), new ExchangeConventions()),
				subscriber: null,
				publisher : new Publisher<MessageContext>(new ChannelFactory(connection), new JsonMessageSerializer(), contextProvider),
				responder: null,
				requester: null
			);
			await subscriber.SubscribeAsync<BasicMessage>((msg, c) =>
			{
				subscribeTcs.SetResult(c.GlobalRequestId);
				return subscribeTcs.Task;
			});

			/* Test */
			publisher.PublishAsync<BasicMessage>();
			await subscribeTcs.Task;

			/* Assert */
			Assert.Equal(subscribeTcs.Task.Result, expectedId);
		}
	}
}
