using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Compatibility.Legacy.Configuration.Exchange;
using RawRabbit.Configuration;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using Xunit;

namespace RawRabbit.IntegrationTests.Compatibility
{
	public class LegacyClientTests : IntegrationTestBase
	{
		private readonly RawRabbitOptions _legacyConfig;

		public LegacyClientTests()
		{
			var clientCfg = RawRabbitConfiguration.Local;
			clientCfg.Exchange.AutoDelete = true;
			clientCfg.Queue.AutoDelete = true;
			_legacyConfig = new RawRabbitOptions
			{
				ClientConfiguration = clientCfg
			};
		}

		[Fact]
		public async Task Should_Pub_Sub_Without_Config()
		{
			/* Setup */
			var publisher = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			var subscriber = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			var message = new BasicMessage { Prop = "Hello, world!" };
			var tsc = new TaskCompletionSource<BasicMessage>();
			MessageContext receivedContext = null;
			var subscription = subscriber.SubscribeAsync<BasicMessage>((msg, context) =>
			{
				receivedContext = context;
				tsc.TrySetResult(msg);
				return Task.FromResult(0);
			});

			/* Test */
			await publisher.PublishAsync(message);
			await tsc.Task;

			/* Assert */
			Assert.Equal(message.Prop, tsc.Task.Result.Prop);
			Assert.NotNull(receivedContext);

			TestChannel.QueueDelete(subscription.QueueName, false, false);
			TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages", false);
			(publisher as IDisposable)?.Dispose();
			(subscriber as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Pub_Sub_With_Custom_Config()
		{
			/* Setup */
			var publisher = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			var subscriber = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			var message = new BasicMessage { Prop = "Hello, world!" };
			var tsc = new TaskCompletionSource<BasicMessage>();
			MessageContext receivedContext = null;
			subscriber.SubscribeAsync<BasicMessage>((msg, context) =>
			{
				receivedContext = context;
				tsc.TrySetResult(msg);
				return Task.FromResult(0);
			}, cfg => cfg
				.WithExchange(e => e
					.WithName("custom_exchange")
					.WithType(ExchangeType.Topic)
					.WithAutoDelete()
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
			Assert.NotNull(receivedContext);

			(publisher as IDisposable)?.Dispose();
			(subscriber as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Pub_Sub_With_Custom_Context()
		{
			/* Setup */
			const string propValue = "This is test message prop";
			_legacyConfig.Plugins = p => p.UseMessageContext(c =>
				new TestMessageContext
				{
					Prop = propValue
				});
			var publisher = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			var subscriber = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient<TestMessageContext>(_legacyConfig);
			var message = new BasicMessage { Prop = "Hello, world!" };
			var tsc = new TaskCompletionSource<BasicMessage>();
			TestMessageContext receivedContext = null;
			subscriber.SubscribeAsync<BasicMessage>((msg, context) =>
			{
				receivedContext = context;
				tsc.TrySetResult(msg);
				return Task.FromResult(0);
			}, cfg => cfg
				.WithExchange(e => e
					.WithName("custom_exchange")
					.WithType(ExchangeType.Topic)
					.WithAutoDelete()
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
			Assert.Equal(receivedContext.Prop, propValue);

			(publisher as IDisposable)?.Dispose();
			(subscriber as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Rpc_Without_Config()
		{
			/* Setup */
			var requester = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			var responder = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			MessageContext receivedContext = null;
			BasicRequest receivedRequest = null;
			var request = new BasicRequest {Number = 3};
			var subscription = responder.RespondAsync<BasicRequest, BasicResponse>((req, context) =>
			{
				receivedRequest = req;
				receivedContext = context;
				return Task.FromResult(new BasicResponse());
			});

			/* Test */
			var response = await requester.RequestAsync<BasicRequest, BasicResponse>(request);

			/* Assert */
			Assert.Equal(receivedRequest.Number, request.Number);
			Assert.NotNull(receivedContext);

			TestChannel.QueueDelete(subscription.QueueName, false, false);
			TestChannel.ExchangeDelete("rawrabbit.integrationtests.testmessages", false);

			(requester as IDisposable)?.Dispose();
			(responder as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Rpc_With_Config()
		{
			/* Setup */
			var requester = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			var responder = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			MessageContext receivedContext = null;
			BasicRequest receivedRequest = null;
			var request = new BasicRequest { Number = 3 };
			var subscription = responder.RespondAsync<BasicRequest, BasicResponse>((req, context) =>
			{
				receivedRequest = req;
				receivedContext = context;
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
			Assert.Equal(receivedRequest.Number, request.Number);
			Assert.NotNull(receivedContext);
			Assert.NotNull(response);

			(requester as IDisposable)?.Dispose();
			(responder as IDisposable)?.Dispose();
		}

		[Fact]
		public async Task Should_Rpc_With_Custom_Context()
		{
			/* Setup */
			const string propValue = "This is test message prop";
			_legacyConfig.Plugins = p => p.UseMessageContext(c =>
				new TestMessageContext
				{
					Prop = propValue
				}
			);
			var requester = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient(_legacyConfig);
			var responder = RawRabbit.Compatibility.Legacy.RawRabbitFactory.CreateClient<TestMessageContext>(_legacyConfig);
			TestMessageContext receivedContext = null;
			BasicRequest receivedRequest = null;
			var sub = responder.RespondAsync<BasicRequest, BasicResponse>((req, context) =>
			{
				receivedContext = context;
				receivedRequest = req;
				return Task.FromResult(new BasicResponse());
			});

			/* Test */
			var response = await requester.RequestAsync<BasicRequest, BasicResponse>(new BasicRequest());

			/* Assert */
			Assert.Equal(receivedContext.Prop, propValue);
			Assert.NotNull(receivedRequest);
			Assert.NotNull(response);

			TestChannel.QueueDelete(sub.QueueName, false, false);
			(requester as IDisposable)?.Dispose();
			(responder as IDisposable)?.Dispose();
		}
	}
}
