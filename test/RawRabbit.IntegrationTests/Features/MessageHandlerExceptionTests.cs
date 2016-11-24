using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Legacy.Subscribe;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context;
using RawRabbit.ErrorHandling;
using RawRabbit.Exceptions;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Serialization;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
	public class MessageHandlerExceptionTests : IDisposable
	{
		private readonly Mock<IErrorHandlingStrategy> _errorHandler;
		private readonly RawRabbit.vNext.Disposable.ILegacyBusClient _client;

		public MessageHandlerExceptionTests()
		{
			_errorHandler = new Mock<IErrorHandlingStrategy>();
			_client = TestClientFactory.CreateNormal(ioc => ioc.AddSingleton(c => _errorHandler.Object));
		}

		public void Dispose()
		{
			_client.Dispose();
		}

		[Fact]
		public async Task Should_Call_Subscribe_Error_Handler_On_Exception_In_Subscribe_Handler()
		{
			/* Setup */
			var exception = new Exception("Oh oh, something when wrong!");
			var realHandler = new DefaultStrategy(null, new NamingConventions(), null, null, null);
			_errorHandler
				.Setup(e => e.ExecuteAsync(
						It.IsAny<Func<Task>>(),
						It.IsAny<Func<Exception, Task>>()
					))
				.Callback((Func<Task> h, Func<Exception, Task> e) => realHandler.ExecuteAsync(h, e));

			_errorHandler
				.Setup(e => e.OnSubscriberExceptionAsync(
					It.IsAny<IRawConsumer>(),
					It.IsAny<SubscriptionConfiguration>(),
					It.IsAny<BasicDeliverEventArgs>(),
					exception
				))
				.Returns(Task.FromResult(true))
				.Verifiable();
			var recieveTcs = new TaskCompletionSource<BasicMessage>();
			_client.SubscribeAsync<BasicMessage>((message, context) =>
			{
				recieveTcs.SetResult(message);
				throw exception;
			}, c => c.WithNoAck());

			/* Test */
			_client.PublishAsync<BasicMessage>();
			await recieveTcs.Task;

			/* Assert */
			_errorHandler.VerifyAll();
		}

		[Fact]
		public async Task Should_Throw_Exception_To_Requester_If_Responder_Throws_Async()
		{
			/* Setup */
			using (var requester = TestClientFactory.CreateNormal())
			using (var responder = TestClientFactory.CreateNormal())
			{
				responder.RespondAsync<BasicRequest, BasicResponse>((request, context) =>
				{
					throw new NotSupportedException("I'll throw this");
				});

				/* Test */
				/* Assert */
				var e = await Assert.ThrowsAsync<MessageHandlerException>(() => requester.RequestAsync<BasicRequest, BasicResponse>());
				Assert.NotNull(e);
			}
		}

		[Fact]
		public async Task Should_Throw_Exception_To_Requester_If_Responder_Throws_Sync()
		{
			/* Setup */
			using (var requester = TestClientFactory.CreateNormal())
			using (var responder = TestClientFactory.CreateNormal())
			{
				var responseException = new NotSupportedException("I'll throw this");
				responder.RespondAsync<BasicRequest, BasicResponse>((request, context) =>
				{
					throw responseException;
				});

				/* Test */
				/* Assert */
				var e = await Assert.ThrowsAsync<MessageHandlerException>(() => requester.RequestAsync<BasicRequest, BasicResponse>());
				Assert.Equal(expected: responseException.Message, actual: e.InnerMessage);
			}
		}

		[Fact]
		public async Task Should_Throw_Exception_If_Deserialization_Of_Response_Fails()
		{
			/* Setup */
			var exception = new Exception("Can not serialize");
			var brokenMsgSerializer = new Mock<IMessageSerializer>();
			brokenMsgSerializer
				.Setup(s => s.Deserialize(It.IsAny<BasicDeliverEventArgs>()))
				.Throws(exception);
			using (var brokenClient = TestClientFactory.CreateNormal(ioc => ioc.AddSingleton(provider => brokenMsgSerializer.Object)))
			{
				brokenClient.RespondAsync<BasicRequest, BasicResponse>(
					(request, context) => Task.FromResult(new BasicResponse()));

				/* Test */
				/* Assert */
				var e = await Assert.ThrowsAsync<Exception>(() => brokenClient.RequestAsync<BasicRequest, BasicResponse>());
				Assert.Equal(e, exception);
			}
		}

		[Fact]
		public async Task Should_Publish_Message_On_Error_Exchange_If_Subscribe_Throws_Exception()
		{
			/* Setup */
			var conventions = new NamingConventions();
			using (var client = TestClientFactory.CreateNormal(ioc => ioc.AddSingleton(c => conventions)))
			{
				var recieveTcs = new TaskCompletionSource<HandlerExceptionMessage>();
				MessageContext firstRecieved = null;
				MessageContext secondRecieved = null;
				client.SubscribeAsync<HandlerExceptionMessage>((message, context) =>
				{
					secondRecieved = context;
					recieveTcs.TrySetResult(message);
					return Task.FromResult(true);
				}, c => c
					.WithExchange(e => e
						.WithName(conventions.ErrorExchangeNamingConvention())
						.WithDurability(false))
					.WithQueue(q => q.WithArgument(QueueArgument.MessageTtl, (int)TimeSpan.FromSeconds(1).TotalMilliseconds).WithAutoDelete())
					.WithRoutingKey("#"));
				client.SubscribeAsync<BasicMessage>((message, context) =>
				{
					firstRecieved = context;
					throw new Exception("Oh oh!");
				});
				var originalMsg = new BasicMessage { Prop = "Hello, world" };

				/* Test */
				client.PublishAsync(originalMsg);
				await recieveTcs.Task;

				/* Assert */
				Assert.Equal(((BasicMessage)recieveTcs.Task.Result.Message).Prop, originalMsg.Prop);
				Assert.NotNull(firstRecieved);
				Assert.NotNull(secondRecieved);
				Assert.Equal(firstRecieved.GlobalRequestId, secondRecieved.GlobalRequestId);
			}
		}

		[Fact]
		public async Task Should_Keep_Consumer_Open_After_Publish_Exception()
		{
			/* Setup */
			using (var client = TestClientFactory.CreateNormal())
			{
				var hasThrownTcs = new TaskCompletionSource<bool>();
				var hasRecievedTcs = new TaskCompletionSource<bool>();
				client.SubscribeAsync<BasicMessage>((message, context) =>
				{
					if (!hasThrownTcs.Task.IsCompleted)
					{
						Timer timer = null;
						timer = new Timer(state =>
						{
							timer?.Dispose();
							hasThrownTcs.SetResult(true);
						}, null, TimeSpan.FromMilliseconds(100), new TimeSpan(-1));
						throw new Exception("Uh uh!");
					}
					else
					{
						hasRecievedTcs.SetResult(true);
					}
					return Task.FromResult(true);
				});

				/* Test */
				client.PublishAsync(new BasicMessage());
				await hasThrownTcs.Task;
				client.PublishAsync(new BasicMessage());
				await hasRecievedTcs.Task;

				/* Assert */
				Assert.True(true);
			}
		}
	}
}
