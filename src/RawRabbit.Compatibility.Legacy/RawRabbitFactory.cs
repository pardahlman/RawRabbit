using RawRabbit.Compatibility.Legacy.Configuration;
using RawRabbit.Context;
using RawRabbit.DependecyInjection;
using RawRabbit.Instantiation;
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
			options.Plugins += builder => builder.UseMessageChaining();
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
				.PublishMessageContext<MessageContext>()
				.RequestMessageContext<MessageContext>()
				.UseMessageChaining(); 
			var simpleIoc = new SimpleDependecyInjection();
			var client = Instantiation.RawRabbitFactory.CreateSingleton(options, simpleIoc, ioc => simpleIoc);
			return new BusClient(client, simpleIoc.GetService<IConfigurationEvaluator>());
		}
	}
}
