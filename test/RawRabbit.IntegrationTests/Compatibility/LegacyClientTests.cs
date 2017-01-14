using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Context;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Compatibility
{
	public class LegacyClientTests : IntegrationTestBase
	{

		[Fact]
		public async Task Should_Pub_Sub_Without_Config()
		{
			/* Setup */
			var publisher = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient();
			var subscriber = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient();
			var message = new BasicMessage { Prop = "Hello, world!" };
			var tsc = new TaskCompletionSource<BasicMessage>();
			MessageContext recievedContext = null;
			var subscription = subscriber.SubscribeAsync<BasicMessage>((msg, context) =>
			{
				recievedContext = context;
				tsc.TrySetResult(msg);
				return Task.FromResult(0);
			});

			/* Test */
			await publisher.PublishAsync(message);
			await tsc.Task;

			/* Assert */
			Assert.Equal(message.Prop, tsc.Task.Result.Prop);
			Assert.NotNull(recievedContext);

			TestChannel.QueueDelete(subscription.QueueName, false, false);
			(publisher as IDisposable)?.Dispose();
			(subscriber as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Pub_Sub_With_Custom_Config()
		{
			/* Setup */
			var publisher = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient();
			var subscriber = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient();
			var message = new BasicMessage { Prop = "Hello, world!" };
			var tsc = new TaskCompletionSource<BasicMessage>();
			MessageContext recievedContext = null;
			subscriber.SubscribeAsync<BasicMessage>((msg, context) =>
			{
				recievedContext = context;
				tsc.TrySetResult(msg);
				return Task.FromResult(0);
			}, cfg => cfg
				.WithExchange(e => e
					.WithName("custom_exchange")
					.WithType(ExchangeType.Topic)
					.WithAutoDelete(false)
				)
				.WithRoutingKey("custom_key")
				.WithQueue(q => q
					.WithName("custom_queue")
					.WithAutoDelete()
				)
			);

			/* Test */
			await publisher.PublishAsync(message, configuration: cfg => cfg
				.WithExchange(e => e
					.AssumeInitialized()
					.WithName("custom_exchange")
					)
				.WithRoutingKey("custom_key"));
			await tsc.Task;

			/* Assert */
			Assert.Equal(message.Prop, tsc.Task.Result.Prop);
			Assert.NotNull(recievedContext);

			(publisher as IDisposable)?.Dispose();
			(subscriber as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Pub_Sub_With_Custom_Context()
		{
			/* Setup */
			const string propValue = "This is test message prop";
			var publisher = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(new RawRabbitOptions
			{
				Plugins = p => p.PublishMessageContext(c =>
					new TestMessageContext
					{
						Prop = propValue
					}
				)
			});
			var subscriber = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient<TestMessageContext>();
			var message = new BasicMessage { Prop = "Hello, world!" };
			var tsc = new TaskCompletionSource<BasicMessage>();
			TestMessageContext recievedContext = null;
			subscriber.SubscribeAsync<BasicMessage>((msg, context) =>
			{
				recievedContext = context;
				tsc.TrySetResult(msg);
				return Task.FromResult(0);
			}, cfg => cfg
				.WithExchange(e => e
					.WithName("custom_exchange")
					.WithType(ExchangeType.Topic)
					.WithAutoDelete(false)
				)
				.WithRoutingKey("custom_key")
				.WithQueue(q => q
					.WithName("custom_queue")
					.WithAutoDelete()
				)
			);

			/* Test */
			await publisher.PublishAsync(message, configuration: cfg => cfg
				.WithExchange(e => e
					.AssumeInitialized()
					.WithName("custom_exchange")
					)
				.WithRoutingKey("custom_key"));
			await tsc.Task;

			/* Assert */
			Assert.Equal(message.Prop, tsc.Task.Result.Prop);
			Assert.Equal(recievedContext.Prop, propValue);

			(publisher as IDisposable)?.Dispose();
			(subscriber as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Rpc_Without_Config()
		{
			/* Setup */
			var requester = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient();
			var responder = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient();
			MessageContext recievedContext = null;
			BasicRequest recievedRequest = null;
			var request = new BasicRequest {Number = 3};
			var subscription = responder.RespondAsync<BasicRequest, BasicResponse>((req, context) =>
			{
				recievedRequest = req;
				recievedContext = context;
				return Task.FromResult(new BasicResponse());
			});

			/* Test */
			var response = await requester.RequestAsync<BasicRequest, BasicResponse>(request);

			/* Assert */
			Assert.Equal(recievedRequest.Number, request.Number);
			Assert.NotNull(recievedContext);

			TestChannel.QueueDelete(subscription.QueueName, false, false);
			(requester as IDisposable)?.Dispose();
			(responder as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Rpc_With_Config()
		{
			/* Setup */
			var requester = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient();
			var responder = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient();
			MessageContext recievedContext = null;
			BasicRequest recievedRequest = null;
			var request = new BasicRequest { Number = 3 };
			var subscription = responder.RespondAsync<BasicRequest, BasicResponse>((req, context) =>
			{
				recievedRequest = req;
				recievedContext = context;
				return Task.FromResult(new BasicResponse());
			}, cfg => cfg.
				WithExchange(e => e
					.WithName("custom_rpc")
					.WithAutoDelete()
				)
				.WithQueue(q => q
					.WithName("rpc_queue")
					.WithAutoDelete()
				)
				.WithRoutingKey("rpc_key")
			);

			/* Test */
			var response = await requester.RequestAsync<BasicRequest, BasicResponse>(request, configuration: cfg => cfg
				.WithExchange(e => e
					.WithName("custom_rpc")
					.AssumeInitialized()
				)
				.WithReplyQueue(q => q
					.WithName("custom_reply")
					.WithAutoDelete()
				)
				.WithRoutingKey("rpc_key")
			);

			/* Assert */
			Assert.Equal(recievedRequest.Number, request.Number);
			Assert.NotNull(recievedContext);
			Assert.NotNull(response);

			(requester as IDisposable)?.Dispose();
			(responder as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Rpc_With_Custom_Context()
		{
			/* Setup */
			const string propValue = "This is test message prop";
			var requester = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(new RawRabbitOptions
			{
				Plugins = p => p.PublishMessageContext(c =>
					new TestMessageContext
					{
						Prop = propValue
					}
				)
			});
			var responder = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient<TestMessageContext>();
			TestMessageContext recievedContext = null;
			BasicRequest recievedRequest = null;
			responder.RespondAsync<BasicRequest, BasicResponse>((req, context) =>
			{
				recievedContext = context;
				recievedRequest = req;
				return Task.FromResult(new BasicResponse());
			});

			/* Test */
			var response = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

			/* Assert */
			Assert.Equal(recievedContext.Prop, propValue);
			Assert.NotNull(recievedRequest);
			Assert.NotNull(response);

			(requester as IDisposable)?.Dispose();
			(responder as IDisposable)?.Dispose();
		}
	}
}
