using System;
using System.Threading.Tasks;
using RawRabbit.Enrichers.ZeroFormatter;
using RawRabbit.Instantiation;
using Xunit;
using ZeroFormatter;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class ZeroFormatterEnricherTests
	{
		[Fact]
		public async Task Should_Publish_And_Subscribe_with_Zero_Formatter()
		{
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseZeroFormatter() }))
			{
				/** Setup **/
				var tcs = new TaskCompletionSource<ZeroFormatterMessage>();
				var message = new ZeroFormatterMessage
				{
					Payload = "Zero formatter!"
				};
				await client.SubscribeAsync<ZeroFormatterMessage>(msg =>
				{
					tcs.TrySetResult(msg);
					return Task.CompletedTask;
				});

				/** Test **/
				await client.PublishAsync(message);
				await tcs.Task;

				/** Assert **/
				Assert.Equal(tcs.Task.Result.Payload, message.Payload);
			}
		}
	}

	[ZeroFormattable]
	public class ZeroFormatterMessage
	{
		[Index(0)]
		public virtual string Payload { get; set; }
	}
}
