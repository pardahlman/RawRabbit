using System;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Configuration.Queue;
using RawRabbit.Enrichers.Polly;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Pipe;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class PolicyEnricherTests
	{
		[Fact]
		public async Task Should_Use_Custom_Policy()
		{
			var defaultCalled = false;
			var customCalled = false;
			var defaultPolicy = Policy
				.Handle<Exception>()
				.FallbackAsync(ct =>
				{
					defaultCalled = true;
					return Task.FromResult(0);
				});
			var declareQueuePolicy = Policy
				.Handle<OperationInterruptedException>()
				.RetryAsync(async (e, retryCount, ctx) =>
				{
					customCalled = true;
					var defaultQueueCfg = ctx.GetPipeContext().GetClientConfiguration().Queue;
					var topology = ctx.GetTopologyProvider();
					var queue = new QueueDeclaration(defaultQueueCfg) { Name = ctx.GetQueueName() };
					await topology.DeclareQueueAsync(queue);
				});

			var options = new vNext.Pipe.RawRabbitOptions
			{
				Plugins = p => p.UsePolly(c => c
					.UsePolicy(defaultPolicy)
					.UsePolicy(declareQueuePolicy, PolicyKeys.QueueBind)
				)
			};

			using (var client = RawRabbitFactory.CreateTestClient(options))
			{
				await client.SubscribeAsync<BasicMessage>(
					message => Task.FromResult(0),
					ctx => ctx.UseConsumerConfiguration(cfg => cfg
						.Consume(c => c
							.FromQueue("does_not_exist"))
					));
			}

			Assert.False(defaultCalled, "The custom retry policy should be called");
			Assert.True(customCalled);
		}
	}
}
