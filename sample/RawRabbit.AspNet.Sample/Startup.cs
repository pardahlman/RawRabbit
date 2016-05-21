using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RawRabbit.Attributes;
using RawRabbit.Common;
using RawRabbit.Extensions.Client;
using RawRabbit.vNext.Logging;

namespace RawRabbit.AspNet.Sample
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddRawRabbit(
					Configuration.GetSection("RawRabbit"),
					ioc => ioc
						.AddSingleton(c => new LoggingFactoryAdapter(c.GetService<ILoggerFactory>())))
						.AddSingleton<IConfigurationEvaluator, AttributeConfigEvaluator>()
				.AddMvc();
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			app.UseMvc();
		}
	}
}
