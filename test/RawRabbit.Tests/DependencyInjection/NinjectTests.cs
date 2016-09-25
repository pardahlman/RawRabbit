using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ninject;
using RawRabbit.Context;
using Xunit;
using RawRabbit.DependencyInjection.Ninject;
using RawRabbit.Logging;

namespace RawRabbit.Tests.DependencyInjection
{
	public class NinjectTests
	{
		[Fact]
		public async Task Should_Be_Able_To_Resolve_IBusClient()
		{
			/* Setup */
			LogManager.CurrentFactory = new VoidLoggerFactory();
			var kernel = new StandardKernel();
			kernel.RegisterRawRabbit("guest:guest@localhost:5672/");

			/* Test */
			var client = kernel.Get<IBusClient>();
			await client.ShutdownAsync(TimeSpan.Zero);

			/* Assert */
			Assert.True(true, "Could resolve");
		}

		[Fact]
		public async Task Should_Be_Able_To_Resolve_BusClient_With_Advanced_Context()
		{
			/* Setup */
			LogManager.CurrentFactory = new VoidLoggerFactory();
			var kernel = new StandardKernel();
			kernel.RegisterRawRabbit<AdvancedMessageContext>("guest:guest@localhost:5672/");

			/* Test */
			var client = kernel.Get<IBusClient<AdvancedMessageContext>>();
			await client.ShutdownAsync(TimeSpan.Zero);

			/* Assert */
			Assert.True(true, "Could resolve");
		}
	}
}
