using System.Threading.Tasks;
using MessagePack;
using RawRabbit.Enrichers.MessagePack;
using RawRabbit.Enrichers.ZeroFormatter;
using RawRabbit.Instantiation;
using Xunit;

namespace RawRabbit.IntegrationTests.Enrichers
{
	public class MessagePackTests
	{
		[Fact]
		public async Task Should_Publish_And_Subscribe_with_Zero_Formatter()
		{
			using (var client = RawRabbitFactory.CreateTestClient(new RawRabbitOptions { Plugins = p => p.UseMessagePack() }))
			{
				/** Setup **/
				var tcs = new TaskCompletionSource<MessagePackMessage>();
				var message = new MessagePackMessage
				{
					TagLine = "Extremely Fast MessagePack Serializer for C#"
				};
				await client.SubscribeAsync<MessagePackMessage>(msg =>
				{
					tcs.TrySetResult(msg);
					return Task.CompletedTask;
				});

				/** Test **/
				await client.PublishAsync(message);
				await tcs.Task;

				/** Assert **/
				Assert.Equal(tcs.Task.Result.TagLine, message.TagLine);
			}
		}
	}

	[MessagePackObject]
	public class MessagePackMessage
	{
		[Key(0)]
		public string TagLine { get; set; }
	}
}
