using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	public class DynamicChannelPoolTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Add_And_Use_Channels()
		{
			/* Setup */
			var channels = new List<Mock<IModel>> {new Mock<IModel>(), new Mock<IModel>(), new Mock<IModel>()};
			foreach (var channel in channels)
			{
				channel
					.Setup(c => c.IsClosed)
					.Returns(false);
			}
			var pool = new DynamicChannelPool();
			pool.Add(channels.Select(c => c.Object));

			/* Test */
			var firstChannel = await pool.GetAsync();
			var secondChannel = await pool.GetAsync();
			var thirdChannel = await pool.GetAsync();

			/* Assert */
			Assert.Equal(firstChannel, channels[0].Object);
			Assert.Equal(secondChannel, channels[1].Object);
			Assert.Equal(thirdChannel, channels[2].Object);
		}

		[Fact]
		public void Should_Not_Throw_Exception_If_Trying_To_Remove_Channel_Not_In_Pool()
		{
			/* Setup */
			var pool = new DynamicChannelPool();
			var channel = new Mock<IModel>();

			/* Test */
			pool.Remove(channel.Object);

			/* Assert */
			Assert.True(true, "Successfully remove a channel not in the pool");
		}

		[Fact]
		public async Task Should_Remove_Channels_Based_On_Count()
		{
			/* Setup */
			var channels = new List<Mock<IModel>>
			{
				new Mock<IModel>{ Name = "First" },
				new Mock<IModel>{ Name = "Second"},
				new Mock<IModel>{ Name = "Third"}
			};
			foreach (var channel in channels)
			{
				channel
					.Setup(c => c.IsOpen)
					.Returns(true);
			}
			var pool = new DynamicChannelPool(channels.Select(c => c.Object));

			/* Test */
			pool.Remove(2);
			var firstChannel = await pool.GetAsync();
			var secondChannel = await pool.GetAsync();
			var thirdChannel = await pool.GetAsync();

			/* Assert */
			Assert.Equal(firstChannel, channels[2].Object);
			Assert.Equal(secondChannel, channels[2].Object);
			Assert.Equal(thirdChannel, channels[2].Object);
		}
	}
}
