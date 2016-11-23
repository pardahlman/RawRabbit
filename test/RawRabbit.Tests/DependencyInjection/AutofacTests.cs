using System;
using System.Threading.Tasks;
using Autofac;
using RawRabbit.Context;
using RawRabbit.DependencyInjection.Autofac;
using RawRabbit.Logging;
using Xunit;

namespace RawRabbit.Tests.DependencyInjection
{
    public class AutofacTests
    {
        [Fact]
        public async Task Should_Be_Able_To_Resolve_IBusClient()
        {
            /* Setup */
            LogManager.CurrentFactory = new VoidLoggerFactory();
            var builder = new ContainerBuilder();
            builder.RegisterRawRabbit("guest:guest@localhost:5672/");
            var container = builder.Build();
            
            /* Test */
            var client = container.Resolve<IBusClient>();
            await client.ShutdownAsync(TimeSpan.Zero);

            /* Assert */
            Assert.True(true, "Could resolve");
        }

        [Fact]
        public async Task Should_Be_Able_To_Resolve_BusClient_With_Advanced_Context()
        {
            /* Setup */
            LogManager.CurrentFactory = new VoidLoggerFactory();
            var builder = new ContainerBuilder();
            builder.RegisterRawRabbit<AdvancedMessageContext>("guest:guest@localhost:5672/");
            var container = builder.Build();

            /* Test */
            var client = container.Resolve<IBusClient<AdvancedMessageContext>>();
            await client.ShutdownAsync(TimeSpan.Zero);

            /* Assert */
            Assert.True(true, "Could resolve");
        }
    }
}
