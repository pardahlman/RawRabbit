using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.vNext;
using RawRabbit.vNext.Pipe;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace RawRabbit.AspNet.Sample
{
	public class Startup
	{
		private readonly string _rootPath;

		public Startup(IHostingEnvironment env)
		{
			_rootPath = env.ContentRootPath;
			var builder = new ConfigurationBuilder()
				.SetBasePath(_rootPath)
				.AddJsonFile("appsettings.json")
				.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddRawRabbit(new RawRabbitOptions
					{
						ClientConfiguration = Configuration.ForRawRabbit(),
						Plugins = p => p
							.UseStateMachine()
							.UseGlobalExecutionId()
					})
				.AddMvc();
		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			Log.Logger = GetConfiguredSerilogger();
			loggerFactory
				.AddSerilog()
				.AddConsole(Configuration.GetSection("Logging"));

			app.UseMvc();
		}

		private ILogger GetConfiguredSerilogger()
		{
			return new LoggerConfiguration()
				.WriteTo.File($"{_rootPath}/Logs/serilog.log", LogEventLevel.Debug)
				.WriteTo.LiterateConsole()
				.CreateLogger();
		}
	}
}
