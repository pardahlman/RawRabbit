using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Exceptions;
using Xunit;

namespace RawRabbit.Tests.Channel
{
	public class ChannelPoolTests
	{
		[Fact]
		public async Task Should_Serve_Open_Channels_In_A_Round_Robin_Manner()
		{
			/* Setup */
			var mockObjects = new List<Mock<IModel>> {new Mock<IModel>(), new Mock<IModel>(), new Mock<IModel>()};
			foreach (var mockObject in mockObjects)
			{
				mockObject.As<IRecoverable>();
				mockObject
					.Setup(m => m.IsOpen)
					.Returns(true);
			}
			var pool = new StaticChannelPool(mockObjects.Select(m => m.Object));

			/* Test */
			var first = await pool.GetAsync();
			var second = await pool.GetAsync();
			var third = await pool.GetAsync();
			var forth = await pool.GetAsync();

			/* Assert */
			Assert.Equal(first, mockObjects[0].Object);
			Assert.Equal(second, mockObjects[1].Object);
			Assert.Equal(third, mockObjects[2].Object);
			Assert.Equal(forth, mockObjects[0].Object);
		}

		[Fact]
		public async Task Should_Not_Serve_Closed_Channels()
		{
			/* Setup */
			var openChannel = new Mock<IModel> { Name = "Always open"};
			var toCloseChannel = new Mock<IModel> { Name = "Will close"};

			openChannel
				.Setup(c => c.IsClosed)
				.Returns(false);

			toCloseChannel
				.SetupSequence(model => model.IsClosed)
				.Returns(false)
				.Returns(true);
			var pool = new StaticChannelPool(new []{openChannel.Object, toCloseChannel.Object});

			/* Test */
			var first = await pool.GetAsync();
			var second = await pool.GetAsync();
			var third = await pool.GetAsync();
			var forth = await pool.GetAsync();

			/* Assert */
			Assert.Equal(first, openChannel.Object);
			Assert.Equal(second, toCloseChannel.Object);
			Assert.Equal(third, openChannel.Object);
			Assert.Equal(forth, openChannel.Object);
		}

		[Fact]
		public async Task Should_Serve_Recovered_Channels()
		{
			/* Setup */
			var openChannel = new Mock<IModel> { Name = "Always open" };
			var closedChannel = new Mock<IModel> { Name = "Will Recover" };
			var recoverable = closedChannel.As<IRecoverable>();

			openChannel
				.Setup(c => c.IsClosed)
				.Returns(false);

			closedChannel
				.SetupSequence(model => model.IsClosed)
				.Returns(true)
				.Returns(true)
				.Returns(false);
			var pool = new StaticChannelPool(new[] { openChannel.Object, closedChannel.Object });

			/* Test */
			var first = await pool.GetAsync();
			var second = await pool.GetAsync();
			recoverable.Raise(model => model.Recovery += null, null, null);
			var third = await pool.GetAsync();
			var forth = await pool.GetAsync();

			/* Assert */
			Assert.Equal(first, openChannel.Object);
			Assert.Equal(second, openChannel.Object);
			Assert.Equal(third, closedChannel.Object);
			Assert.Equal(forth, openChannel.Object);
		}

		[Fact]
		public async Task Should_Throw_Exception_If_All_Channels_Are_Closed_And_None_Is_Recoverable()
		{
			/* Setup */
			var mockObjects = new List<Mock<IModel>> { new Mock<IModel>(), new Mock<IModel>(), new Mock<IModel>() };
			foreach (var mockObject in mockObjects)
			{
				mockObject
					.Setup(m => m.IsClosed)
					.Returns(true);
			}
			var pool = new StaticChannelPool(mockObjects.Select(m => m.Object));

			/* Test */
			try
			{
				await pool.GetAsync();
				Assert.True(false, $"{nameof(ChannelAvailabilityException)} should be thrown");
			}
			catch (ChannelAvailabilityException e)
			{
				Assert.True(true, e.Message);
			}
		}

		[Fact]
		public async Task Should_Not_Throw_If_All_Channels_Are_Closed_But_At_Least_One_Is_Recoverable()
		{
			/* Setup */
			var closedChannel = new Mock<IModel> { Name = "Always open" };
			var recoverableChannel = new Mock<IModel> { Name = "Will Recover" };
			var recoverable = recoverableChannel.As<IRecoverable>();

			closedChannel
				.Setup(c => c.IsClosed)
				.Returns(true);

			recoverableChannel
				.SetupSequence(model => model.IsClosed)
				.Returns(true)
				.Returns(true)
				.Returns(false);

			var pool = new StaticChannelPool(new[] { recoverableChannel.Object, closedChannel.Object });

			/* Test */
			var channelTask = pool.GetAsync();
			channelTask.Wait(TimeSpan.FromMilliseconds(20));
			Assert.False(channelTask.IsCompleted, "No channels should be open,");

			recoverable.Raise(r => r.Recovery += null, null, null);
			await channelTask;

			Assert.Equal(recoverableChannel.Object, channelTask.Result);
		}

		[Fact]
		public void Should_Be_Able_To_Have_Multiple_Pending_Requests()
		{
			/* Setup */
			const int numberOfCalls = 200;
			var taskArray = new Task[numberOfCalls];
			var mockObjects = new List<Mock<IModel>> { new Mock<IModel>(), new Mock<IModel>(), new Mock<IModel>() };
			foreach (var mockObject in mockObjects)
			{
				mockObject.As<IRecoverable>();
				mockObject
					.Setup(m => m.IsClosed)
					.Returns(false);
			}
			var pool = new StaticChannelPool(mockObjects.Select(m => m.Object));


			/* Test */
			for (var i = 0; i < numberOfCalls; i++)
			{
				taskArray[i] = pool.GetAsync();
			}

			Task.WaitAll(taskArray);

			Assert.True(true, "No exception thrown with multiple pending");
		}

		[Fact]
		public async Task Should_Throw_Exception_If_All_Channels_Are_Closed_And_Close_Reason_For_All_Recoverable_Channels_Are_Application()
		{
			/* Setup */
			var mockObjects = new List<Mock<IModel>> { new Mock<IModel>(), new Mock<IModel>(), new Mock<IModel>() };
			foreach (var mockObject in mockObjects)
			{
				mockObject.As<IRecoverable>();
				mockObject
					.Setup(m => m.IsClosed)
					.Returns(true);
				mockObject
					.Setup(c => c.CloseReason)
					.Returns(new ShutdownEventArgs(ShutdownInitiator.Application, 0, ""));
			}
			var pool = new StaticChannelPool(mockObjects.Select(m => m.Object));

			/* Test */
			try
			{
				var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
				await pool.GetAsync(cts.Token);
				Assert.True(false, $"Should throw {nameof(ChannelAvailabilityException)}.");
			}
			catch (ChannelAvailabilityException e)
			{
				Assert.True(true, e.Message);
			}
		}

		[Fact]
		public async Task Should_Be_Able_To_Cancel_With_Token()
		{
			/* Setup */
			var closedChannel = new Mock<IModel> { Name = "Closed Channel"};
			closedChannel.As<IRecoverable>();
			closedChannel
				.Setup(m => m.IsClosed)
				.Returns(true);
			var pool = new StaticChannelPool(new []{closedChannel.Object});
			var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

			/* Test */
			/* Assert */
			try
			{
				await pool.GetAsync(cts.Token);
				Assert.True(false, $"Task cancelled before completed, should throw {nameof(OperationCanceledException)}");
			}
			catch (OperationCanceledException e)
			{
				Assert.True(true, e.Message);
			}
		}

		[Fact]
		public async Task Should_Throw_Exception_If_Recoverable_Channel_Is_Closed_By_Application()
		{
			/* Setup */
			var closedChannel = new Mock<IModel> { Name = "Closed Channel" };
			closedChannel.As<IRecoverable>();
			closedChannel
				.SetupSequence(m => m.IsClosed)
				.Returns(false)
				.Returns(true);
			var pool = new StaticChannelPool(new[] { closedChannel.Object });
			closedChannel.Raise(c =>c.ModelShutdown += null, null, new ShutdownEventArgs(ShutdownInitiator.Application, 0, string.Empty));

			/* Test */
			/* Assert */
			try
			{
				await pool.GetAsync();
				Assert.True(false, $"Task completed, should have thrown {nameof(ChannelAvailabilityException)}");
			}
			catch (ChannelAvailabilityException e)
			{
				Assert.True(true, e.Message);
			}
		}
	}
}
