using System;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using RawRabbit.Advanced;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.DependencyInjection.Autofac;
using RawRabbit.Instantiation;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Operations.StateMachine;
using RawRabbit.Pipe.Extensions;
using Xunit;

namespace RawRabbit.IntegrationTests.DependecyInjection
{
	public class AutofacTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Resolve_Client_From_Autofac()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			builder.RegisterRawRabbit(new RawRabbitOptions());
			var container = builder.Build();

			/* Test */
			var client = container.Resolve<IBusClient>();
			var disposer = container.Resolve<IResourceDisposer>();

			/* Assert */
			disposer.Dispose();
			Assert.True(true);
		}

		[Fact]
		public async Task Should_Be_Able_To_Publish_Message_From_Resolved_Client()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			builder.RegisterRawRabbit();
			var container = builder.Build();

			/* Test */
			var client = container.Resolve<IBusClient>();
			await client.PublishAsync(new BasicMessage());
			await client.DeleteExchangeAsync<BasicMessage>();
			var disposer = container.Resolve<IResourceDisposer>();

			/* Assert */
			disposer.Dispose();
			Assert.True(true);
		}

		[Fact]
		public async Task Should_Honor_Client_Configuration()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			var config = RawRabbitConfiguration.Local;
			config.VirtualHost = "/foo";

			/* Test */
			await Assert.ThrowsAsync<DependencyResolutionException>(async () =>
			{
				builder.RegisterRawRabbit(new RawRabbitOptions {ClientConfiguration = config});
				var container = builder.Build();
				var client = container.Resolve<IBusClient>();
				await client.CreateChannelAsync();
			});
			

			/* Assert */
			Assert.True(true);
		}

		[Fact]
		public async Task Should_Be_Able_To_Resolve_Client_With_Plugins_From_Autofac()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			builder.RegisterRawRabbit(new RawRabbitOptions
			{
				Plugins = p => p.UseStateMachine()
			});
			var container = builder.Build();

			/* Test */
			var client = container.Resolve<IBusClient>();
			var disposer = container.Resolve<IResourceDisposer>();

			/* Assert */
			disposer.Dispose();
			Assert.True(true);
		}
	}
}
