using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.Subscribe.Context;
using Xunit;

namespace RawRabbit.IntegrationTests.PublishAndSubscribe
{
    public class ErrorOnExceptionTests
    {
	    [Fact]
	    public async Task All_Good_In_Custom_Subscription()
	    {
			using (var publisher = RawRabbitFactory.CreateTestClient())
			using (var subscriber = RawRabbitFactory.CreateTestClient())
			using (var deadletterSubscriber = RawRabbitFactory.CreateTestClient())
			{
				var messageId = Guid.NewGuid().ToString();

				await subscriber
					.CustomSubscribeAsync<BasicMessage>(
						async message =>
						{
							throw new DivideByZeroException();
						}						)
					.ConfigureAwait(false);

				var errorExchange = new NamingConventions().ErrorExchangeNamingConvention();

				var tcs = new TaskCompletionSource<BasicMessage>();

				await deadletterSubscriber
					.SubscribeAsync<BasicMessage>(
						async message =>
						{
							if (message.Prop == messageId)
								tcs.TrySetResult(message);
						},
						pipe => pipe
							.UseSubscribeConfiguration(cfg => cfg
								.FromDeclaredQueue(q => q.WithName("q"))
								.OnDeclaredExchange(ex => ex.WithName(errorExchange))
							)
						);

				await publisher
					.PublishAsync(new BasicMessage { Prop = messageId })
					.ConfigureAwait(false);

				using (new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token.Register(() => tcs.TrySetCanceled()))
				{
					await tcs.Task.ConfigureAwait(false);
				}
			}
		}

	    [Fact]
	    public async Task Continuation_Swallows_Exception_In_Existing()
	    {
		    using (var publisher = RawRabbitFactory.CreateTestClient())
		    using (var subscriber = RawRabbitFactory.CreateTestClient())
		    using (var deadletterSubscriber = RawRabbitFactory.CreateTestClient())
		    {
			    var messageId = Guid.NewGuid().ToString();

			    await subscriber
				    .SubscribeAsync<BasicMessage>(
					    async message =>
					    {
						    throw new DivideByZeroException();
					    })
				    .ConfigureAwait(false);

			    var errorExchange = new NamingConventions().ErrorExchangeNamingConvention();

			    var tcs = new TaskCompletionSource<BasicMessage>();

			    await deadletterSubscriber
				    .SubscribeAsync<BasicMessage>(
					    async message =>
					    {
						    if (message.Prop == messageId)
							    tcs.TrySetResult(message);
					    },
					    pipe => pipe
						    .UseSubscribeConfiguration(cfg => cfg
							    .FromDeclaredQueue(q => q.WithName("q"))
							    .OnDeclaredExchange(ex => ex.WithName(errorExchange))
						    )
				    );

			    await publisher
				    .PublishAsync(new BasicMessage { Prop = messageId })
				    .ConfigureAwait(false);

			    using (new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token.Register(() => tcs.TrySetCanceled()))
			    {
				    await Assert.ThrowsAsync<TaskCanceledException>(async () => await tcs.Task.ConfigureAwait(false)).ConfigureAwait(false);
			    }
		    }
	    }
	}

	public static class Ex
	{
		public static Task CustomSubscribeAsync<TMessage>(this IBusClient client, Func<TMessage, Task> subscribeMethod,
			Action<ISubscribeContext> context = null, CancellationToken ct = default(CancellationToken))
		{
			async Task<Acknowledgement> Subscribe(TMessage message)
			{
				await subscribeMethod(message).ConfigureAwait(false);
				return new Ack();
			}

			return client.SubscribeAsync<TMessage>(Subscribe, context, ct);
		}
	}
}
