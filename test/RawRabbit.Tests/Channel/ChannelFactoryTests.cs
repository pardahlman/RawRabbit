using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Configuration;
using RawRabbit.Exceptions;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	public class ChannelFactoryTests
	{
		[Fact]
		public async Task Should_Throw_Exception_If_Connection_Is_Closed_By_Application()
		{
			/* Setup */
			var connectionFactroy = new Mock<IConnectionFactory>();
			var connection = new Mock<IConnection>();
			connectionFactroy
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()))
				.Returns(connection.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(false);
			connection
				.Setup(c => c.CloseReason)
				.Returns(new ShutdownEventArgs(ShutdownInitiator.Application, 0, string.Empty));
			var channelFactory = new ChannelFactory(connectionFactroy.Object, RawRabbitConfiguration.Local);

			/* Test */
			/* Assert */
			try
			{
				await channelFactory.CreateChannelAsync();
				Assert.True(false, $"Connection is closed by Application, expected {nameof(ChannelAvailabilityException)}.");
			}
			catch (ChannelAvailabilityException e)
			{
				Assert.True(true, e.Message);
			}
		}

		[Fact]
		public async Task Should_Throw_Exception_If_Connection_Is_Closed_By_Lib_But_Is_Not_Recoverable()
		{
			/* Setup */
			var connectionFactroy = new Mock<IConnectionFactory>();
			var connection = new Mock<IConnection>();
			connectionFactroy
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()))
				.Returns(connection.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(false);
			connection
				.Setup(c => c.CloseReason)
				.Returns(new ShutdownEventArgs(ShutdownInitiator.Library, 0, string.Empty));
			var channelFactory = new ChannelFactory(connectionFactroy.Object, RawRabbitConfiguration.Local);

			/* Test */
			/* Assert */
			try
			{
				await channelFactory.CreateChannelAsync();
				Assert.True(false, $"Connection is closed by Application, expected {nameof(ChannelAvailabilityException)}.");
			}
			catch (ChannelAvailabilityException e)
			{
				Assert.True(true, e.Message);
			}
		}

		[Fact]
		public async Task Should_Return_Channel_From_Connection()
		{
			/* Setup */
			var channel = new Mock<IModel>();
			var connectionFactroy = new Mock<IConnectionFactory>();
			var connection = new Mock<IConnection>();
			connectionFactroy
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()))
				.Returns(connection.Object);
			connection
				.Setup(c => c.CreateModel())
				.Returns(channel.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(true);
			var channelFactory = new ChannelFactory(connectionFactroy.Object, RawRabbitConfiguration.Local);

			/* Test */
			var retrievedChannel = await channelFactory.CreateChannelAsync();

			/* Assert */
			Assert.Equal(channel.Object, retrievedChannel);
		}

		[Fact]
		public async Task Should_Wait_For_Connection_To_Recover_Before_Returning_Channel()
		{
			/* Setup */
			var channel = new Mock<IModel>();
			var connectionFactroy = new Mock<IConnectionFactory>();
			var connection = new Mock<IConnection>();
			var recoverable = connection.As<IRecoverable>();
			connectionFactroy
				.Setup(c => c.CreateConnection(
					It.IsAny<List<string>>()))
				.Returns(connection.Object);
			connection
				.Setup(c => c.CreateModel())
				.Returns(channel.Object);
			connection
				.Setup(c => c.IsOpen)
				.Returns(false);
			var channelFactory = new ChannelFactory(connectionFactroy.Object, RawRabbitConfiguration.Local);

			/* Test */
			/* Assert */
			var channelTask = channelFactory.CreateChannelAsync();
			channelTask.Wait(TimeSpan.FromMilliseconds(30));
			Assert.False(channelTask.IsCompleted);

			recoverable.Raise(r => r.Recovery += null, null, null);
			await channelTask;

			Assert.Equal(channel.Object, channelTask.Result);
		}
	}
}
