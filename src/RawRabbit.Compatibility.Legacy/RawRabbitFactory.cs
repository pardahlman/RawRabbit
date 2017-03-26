using System;
using RawRabbit.Compatibility.Legacy.Configuration;
using RawRabbit.DependecyInjection;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using RawRabbitConfiguration = RawRabbit.Configuration.RawRabbitConfiguration;

namespace RawRabbit.Compatibility.Legacy
{
	public class RawRabbitFactory
	{
		public static IBusClient<TMessageContext> CreateClient<TMessageContext>(RawRabbitOptions options = null)
			where TMessageContext : IMessageContext
		{
			options = options ?? new RawRabbitOptions();
			options.DependencyInjection = options.DependencyInjection ?? (register => { });
			options.DependencyInjection += register => register.AddSingleton<IConfigurationEvaluator, ConfigurationEvaluator>();
			options.ClientConfiguration = options?.ClientConfiguration ?? RawRabbitConfiguration.Local;
			options.Plugins = options.Plugins ?? (builder => { });
			options.Plugins += builder => builder
				.UseMessageContext(context => new MessageContext { GlobalRequestId = Guid.NewGuid() })
				.UseContextForwarding();
			var simpleIoc = new SimpleDependecyInjection();
			var client = Instantiation.RawRabbitFactory.CreateSingleton(options, simpleIoc, ioc => simpleIoc);
			return new BusClient<TMessageContext>(client, simpleIoc.GetService<IConfigurationEvaluator>());
		}

		public static IBusClient CreateClient(RawRabbitOptions options = null)
		{
			options = options ?? new RawRabbitOptions();
			options.DependencyInjection = options.DependencyInjection ?? (register => { });
			options.DependencyInjection += register => register.AddSingleton<IConfigurationEvaluator, ConfigurationEvaluator>();
			options.ClientConfiguration = options?.ClientConfiguration ?? RawRabbitConfiguration.Local;
			options.Plugins = options.Plugins ?? (builder => { });
			options.Plugins += builder => builder
				.UseMessageContext(context => new MessageContext {GlobalRequestId = Guid.NewGuid()})
				.UseContextForwarding();
			var simpleIoc = new SimpleDependecyInjection();
			var client = Instantiation.RawRabbitFactory.CreateSingleton(options, simpleIoc, ioc => simpleIoc);
			return new BusClient(client, simpleIoc.GetService<IConfigurationEvaluator>());
		}
	}
}
