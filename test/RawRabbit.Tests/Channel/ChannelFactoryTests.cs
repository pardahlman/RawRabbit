using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Configuration;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	public class ChannelFactoryTests
	{
		private readonly Mock<IConnectionFactory> _connectionFactory;
		private readonly RawRabbitConfiguration _config;
		private readonly ChannelFactoryConfiguration _channelConfig;
		private readonly Mock<IConnection> _connection;
		private readonly Mock<IModel> _firstChannel;
		private readonly Mock<IModel> _secondChannel;
		private readonly Mock<IModel> _thirdChannel;

		public ChannelFactoryTests()
		{
			_connectionFactory = new Mock<IConnectionFactory>();
			_connection = new Mock<IConnection>();
			_firstChannel = new Mock<IModel>();
			_secondChannel = new Mock<IModel>();
			_thirdChannel = new Mock<IModel>();
			_config = RawRabbitConfiguration.Local;
			_channelConfig = ChannelFactoryConfiguration.Default;

			_connectionFactory
				.Setup(c => c.CreateConnection(It.IsAny<IList<string>>()))
				.Returns(_connection.Object);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(3)]
		public async Task Should_Create_Initial_Channels_Based_On_Config(int initialChannelCount)
		{
			/* Setup */
			_channelConfig.MaxChannelCount = initialChannelCount;
			_channelConfig.InitialChannelCount = initialChannelCount;
			_connection
				.Setup(c => c.IsOpen)
				.Returns(true);
			_connection
				.Setup(c=> c.CreateModel())
				.Returns(_firstChannel.Object)
				.Verifiable();

			/* Test */
			var factory = new ChannelFactory(_connectionFactory.Object, _config, _channelConfig);

			/* Assert */
			_connection.Verify(c => c.CreateModel(), Times.Exactly(initialChannelCount));
		}

		[Fact]
		public async Task Should_Round_Robin_Between_Existing_Channels()
		{
			/* Setup */
			_channelConfig.MaxChannelCount = 3;
			_channelConfig.InitialChannelCount = 3;
			_connection
				.Setup(c => c.IsOpen)
				.Returns(true);
			_connection
				.SetupSequence(c => c.CreateModel())
				.Returns(_thirdChannel.Object)
				.Returns(_firstChannel.Object)
				.Returns(_secondChannel.Object);
			_firstChannel
				.Setup(c => c.IsOpen)
				.Returns(true);
			_secondChannel
				.Setup(c => c.IsOpen)
				.Returns(true);
			_thirdChannel
				.Setup(c => c.IsOpen)
				.Returns(true);
			var factory = new ChannelFactory(_connectionFactory.Object, _config, _channelConfig);

			/* Test */
			var first = await factory.GetChannelAsync();
			var second = await factory.GetChannelAsync();
			var third = await factory.GetChannelAsync();
			var firstAgain = await factory.GetChannelAsync();
			var secondAgain = await factory.GetChannelAsync();
			var thirdAgain = await factory.GetChannelAsync();

			/* Assert */
			Assert.Equal(first, _firstChannel.Object);
			Assert.Equal(second, _secondChannel.Object);
			Assert.Equal(third, _thirdChannel.Object);
			Assert.Equal(firstAgain, _firstChannel.Object);
			Assert.Equal(secondAgain, _secondChannel.Object);
			Assert.Equal(thirdAgain, _thirdChannel.Object);
		}

		[Fact]
		public async Task Should_Not_Return_Close_Channel()
		{
			/* Setup */
			_channelConfig.MaxChannelCount = 3;
			_channelConfig.InitialChannelCount = 3;
			_connection
				.Setup(c => c.IsOpen)
				.Returns(true);
			_connection
				.SetupSequence(c => c.CreateModel())
				.Returns(_thirdChannel.Object)
				.Returns(_firstChannel.Object)
				.Returns(_secondChannel.Object);
			_firstChannel
				.Setup(c => c.IsOpen)
				.Returns(true);
			_secondChannel
				.SetupSequence( c => c.IsOpen)
				.Returns(true)
				.Returns(false);
			_thirdChannel
				.Setup(c => c.IsOpen)
				.Returns(true);
			_secondChannel
				.Setup(c => c.CloseReason)
				.Returns(new ShutdownEventArgs(ShutdownInitiator.Application, 200, "Test"));
			var factory = new ChannelFactory(_connectionFactory.Object, _config, _channelConfig);

			/* Test */
			var first = await factory.GetChannelAsync();
			var second = await factory.GetChannelAsync();
			var third = await factory.GetChannelAsync();
			var firstAgain = await factory.GetChannelAsync();
			var thirdAgain = await factory.GetChannelAsync();

			/* Assert */
			Assert.Equal(first, _firstChannel.Object);
			Assert.Equal(second, _secondChannel.Object);
			Assert.Equal(third, _thirdChannel.Object);
			Assert.Equal(firstAgain, _firstChannel.Object);
			Assert.Equal(thirdAgain, _thirdChannel.Object);
		}
	}
}
