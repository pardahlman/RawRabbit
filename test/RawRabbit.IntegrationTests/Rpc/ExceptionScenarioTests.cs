using System;
using System.Threading.Tasks;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Exceptions;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
using RawRabbit.vNext.Pipe;
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
		public async Task Should_Propegate_Responder_Exception_To_Requester_When_Request_Handler_Is_Async()
		{
			using (var requester = RawRabbitFactory.CreateTestClient())
			using (var responder = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				Func<BasicRequest, Task<BasicResponse>> errorHandler = async request =>
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

		[Fact]
		public async Task Should_Publish_Message_To_Error_Exchange()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage
				{
					Prop = "I'm handled, and sent to Error Exchange"
				};
				var tsc = new TaskCompletionSource<BasicMessage>();
				Func<BasicMessage, Task> errorHandler = request =>
				{
					throw new Exception("Kaboom");
				};
				await subscriber.SubscribeAsync(errorHandler);
				await subscriber.SubscribeAsync<BasicMessage>(msg =>
				{
					tsc.TrySetResult(msg);
					return Task.FromResult(0);
				}, ctx => ctx
					.UseConsumerConfiguration(cfg => cfg
						.FromDeclaredQueue(q => q.WithName("custom_error_queue"))
						.OnDeclaredExchange(e => e.WithName("default_error_exchange"))
				));

				/* Test */
				await publisher.PublishAsync(message);
				await tsc.Task;

				/* Assert */
				Assert.Equal(message.Prop, tsc.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Publish_Message_To_Error_Exchange_When_Subscriber_Has_Context()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseMessageContext<MessageContext>()}))
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var message = new BasicMessage
				{
					Prop = "I'm handled, and sent to Error Exchange"
				};
				var tsc = new TaskCompletionSource<BasicMessage>();
				Func<BasicMessage, MessageContext, Task> errorHandler = (msg, ctx) =>
				{
					throw new Exception("Kaboom");
				};
				await subscriber.SubscribeAsync(errorHandler);
				await subscriber.SubscribeAsync<BasicMessage, MessageContext>((msg, ctx) =>
				{
					tsc.TrySetResult(msg);
					return Task.FromResult(0);
				}, ctx => ctx.UseConsumerConfiguration(cfg => cfg
					.FromDeclaredQueue(q => q.WithName("custom_error_queue"))
					.OnDeclaredExchange(e => e.WithName("default_error_exchange"))
				));

				/* Test */
				await publisher.PublishAsync(message);
				await tsc.Task;

				/* Assert */
				Assert.Equal(message.Prop, tsc.Task.Result.Prop);
			}
		}
	}
}
