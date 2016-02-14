using System.Threading.Tasks;
using Autofac;
using RawRabbit.Context;
using RawRabbit.DependencyInjection.Autofac;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection
{
	public class AutofacTests
	{
		[Fact]
		public void Should_Be_Able_To_Resolve_IBusClient()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			builder.RegisterRawRabbit("guest:guest@localhost:5672/");
			var container = builder.Build();
			
			/* Test */
			var client = container.Resolve<IBusClient>();

			/* Assert */
			Assert.True(true, "Could resolve");
		}

		[Fact]
		public void Should_Be_Able_To_Resolve_BusClient_With_Advanced_Context()
		{
			/* Setup */
			var builder = new ContainerBuilder();
			builder.RegisterRawRabbit<AdvancedMessageContext>("guest:guest@localhost:5672/");
			var container = builder.Build();

			/* Test */
			var client = container.Resolve<IBusClient<AdvancedMessageContext>>();

			/* Assert */
			Assert.True(true, "Could resolve");
		}
	}
}
