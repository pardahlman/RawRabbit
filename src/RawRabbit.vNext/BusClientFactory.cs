using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.vNext.Disposable;

namespace RawRabbit.vNext
{
    public class BusClientFactory
    {
        public static Disposable.IBusClient CreateDefault(TimeSpan requestTimeout)
        {
            var cfg = RawRabbitConfiguration.Local;
            cfg.RequestTimeout = requestTimeout;
            return CreateDefault(cfg);
        }

        public static Disposable.IBusClient CreateDefault(RawRabbitConfiguration config)
        {
            var addCfg = new Action<IServiceCollection>(s => s.AddSingleton(p => config));
            var services = new ServiceCollection().AddRawRabbit(null, addCfg);
            return CreateDefault(services);
        }

        public static Disposable.IBusClient CreateDefault(Action<IServiceCollection> custom = null)
        {
            var services = new ServiceCollection().AddRawRabbit(null, custom);
            return CreateDefault(services);
        }

        public static Disposable.IBusClient CreateDefault(Action<IConfigurationBuilder> config, Action<IServiceCollection> custom)
        {
            var services = new ServiceCollection().AddRawRabbit(config, custom);
            return CreateDefault(services);
        }

        public static Disposable.IBusClient CreateDefault(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var client = serviceProvider.GetService<IBusClient>();
            return new Disposable.BusClient(client);
        }

        public static Disposable.IBusClient<TMessageContext> CreateDefault<TMessageContext>(Action<IConfigurationBuilder> config = null, Action<IServiceCollection> custom = null) where TMessageContext : IMessageContext
        {
            var serviceProvider = new ServiceCollection()
                .AddRawRabbit<TMessageContext>(config, custom)
                .BuildServiceProvider();

            var client = serviceProvider.GetService<IBusClient<TMessageContext>>();
            return new BusClient<TMessageContext>(client);
        }
    }
}
