using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;
using RawRabbit.Enrichers.MessageContext.Subscribe;
using RawRabbit.Enrichers.QueueSuffix;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
	public class ConfigurationTests
	{
		[Fact]
		public async Task Should_Work_Without_Any_Additional_Configuration()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				await subscriber.SubscribeAsync<BasicMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(true);
				});
				var message = new BasicMessage {Prop = "Hello, world!"};

				/* Test */
				await publisher.PublishAsync(message);
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Publish_With_Custom_Header()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicDeliverEventArgs>();
				await subscriber.SubscribeAsync<BasicMessage, BasicDeliverEventArgs>((recieved, args) =>
				{
					recievedTcs.TrySetResult(args);
					return Task.FromResult(true);
				}, ctx => ctx.UseMessageContext(c => c.GetDeliveryEventArgs()));
				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message, ctx => ctx
					.UsePublisherConfiguration(cfg => cfg
						.WithProperties(props => props.Headers.Add("foo", "bar"))));
				await recievedTcs.Task;

				/* Assert */
				Assert.True(recievedTcs.Task.Result.BasicProperties.Headers.ContainsKey("foo"));
			}
		}

		[Fact]
		public async Task Should_Honor_Exchange_Name_Configuration()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				await subscriber.SubscribeAsync<BasicMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(true);
				}, ctx => ctx
					.UseConsumerConfiguration(cfg => cfg
						.OnDeclaredExchange(e=> e
							.WithName("custom_exchange")
						))
				);

				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message, ctx => ctx.UsePublisherConfiguration(cfg => cfg.OnExchange("custom_exchange")));
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Honor_Complex_Configuration()
		{
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var recievedTcs = new TaskCompletionSource<BasicMessage>();
				await subscriber.SubscribeAsync<BasicMessage>(recieved =>
				{
					recievedTcs.TrySetResult(recieved);
					return Task.FromResult(true);
				}, ctx => ctx
					.UseConsumerConfiguration(cfg => cfg
						.Consume(c => c
							.WithRoutingKey("custom_key")
							.WithConsumerTag("custom_tag")
							.WithPrefetchCount(2)
							.WithNoLocal(false))
						.FromDeclaredQueue(q => q
							.WithName("custom_queue")
							.WithAutoDelete()
							.WithArgument(QueueArgument.DeadLetterExchange, "dlx"))
						.OnDeclaredExchange(e=> e
							.WithName("custom_exchange")
							.WithType(ExchangeType.Topic))
				));

				var message = new BasicMessage { Prop = "Hello, world!" };

				/* Test */
				await publisher.PublishAsync(message, ctx => ctx
					.UsePublisherConfiguration(cfg => cfg
						.OnExchange("custom_exchange")
						.WithRoutingKey("custom_key")
				));
				await recievedTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, recievedTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Create_Unique_Queues_With_Naming_Suffix()
		{
			var options = new RawRabbitOptions
			{
				Plugins = ioc => ioc
					.UseApplicationQueueSuffix()
					.UseQueueSuffix()
			};
			using (var firstSubscriber = RawRabbitFactory.CreateTestClient(options))
			using (var secondSubscriber = RawRabbitFactory.CreateTestClient(options))
			using (var publisher = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var firstTcs = new TaskCompletionSource<BasicMessage>();
				var secondTcs = new TaskCompletionSource<BasicMessage>();
				var message = new BasicMessage {Prop = "I'm delivered twice."};
				await firstSubscriber.SubscribeAsync<BasicMessage>(msg =>
				{
					firstTcs.TrySetResult(msg);
					return Task.FromResult(0);
				}, ctx => ctx.UseCustomQueueSuffix("first")
				);

				await secondSubscriber.SubscribeAsync<BasicMessage>(msg =>
				{
					secondTcs.TrySetResult(msg);
					return Task.FromResult(0);
				}, ctx => ctx.UseCustomQueueSuffix("second")
				);

				/* Test */
				await publisher.PublishAsync(message);
				await firstTcs.Task;
				await secondTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, firstTcs.Task.Result.Prop);
				Assert.Equal(message.Prop, secondTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Not_Throw_Exception_When_Queue_Name_Is_Long()
		{
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			using (var publisher = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var msgTcs = new TaskCompletionSource<BasicMessage>();
				var message = new BasicMessage { Prop = "I'm delivered to queue with truncated name" };
				var queueName = string.Empty;
				while (queueName.Length < 254)
				{
					queueName = queueName + "this_is_part_of_a_long_queue_name";
				}
				await subscriber.SubscribeAsync<BasicMessage>(msg =>
				{
					msgTcs.TrySetResult(msg);
					return Task.FromResult(0);
				}, ctx => ctx
					.UseConsumerConfiguration(cfg => cfg
						.FromDeclaredQueue(q => q.WithName(queueName).WithAutoDelete())
					)
				);

				/* Test */
				await publisher.PublishAsync(message);
				await msgTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, msgTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Not_Throw_Exception_When_Exchange_Name_Is_Long()
		{
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			using (var publisher = RawRabbitFactory.CreateTestClient())
			{
				/* Setup */
				var msgTcs = new TaskCompletionSource<BasicMessage>();
				var message = new BasicMessage { Prop = "I'm delivered on an exchange with truncated name" };
				var exchangeName = string.Empty;
				while (exchangeName.Length < 254)
				{
					exchangeName = exchangeName + "this_is_part_of_a_long_exchange_name";
				}

				await subscriber.SubscribeAsync<BasicMessage>(msg =>
				{
					msgTcs.TrySetResult(msg);
					return Task.FromResult(0);
				}, ctx => ctx
					.UseConsumerConfiguration(cfg => cfg
						.OnDeclaredExchange(e => e.WithName(exchangeName).WithAutoDelete())
					)
				);

				/* Test */
				await publisher.PublishAsync(message, ctx => ctx
					.UsePublishAcknowledge()
					.UsePublisherConfiguration(c => c.OnExchange(exchangeName))
				);
				await msgTcs.Task;

				/* Assert */
				Assert.Equal(message.Prop, msgTcs.Task.Result.Prop);
			}
		}

		[Fact]
		public async Task Should_Consume_Message_Already_In_Queue()
		{
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			using (var publisher = RawRabbitFactory.CreateTestClient())
			{
				var msgTcs = new TaskCompletionSource<BasicMessage>();
				var msg = new BasicMessage { Prop = Guid.NewGuid().ToString() };
				await subscriber.DeclareQueueAsync<BasicMessage>();
				await subscriber.DeclareExchangeAsync<BasicMessage>();
				await subscriber.BindQueueAsync<BasicMessage>();
				await publisher.PublishAsync(msg);
				await subscriber.SubscribeAsync<BasicMessage>(message =>
				{
					msgTcs.TrySetResult(message);
					return Task.FromResult(true);
				});
				await msgTcs.Task;
				Assert.Equal(msg.Prop, msgTcs.Task.Result.Prop);
			}
		}
	}
}
