using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RawRabbit.Configuration;
using RawRabbit.Enrichers.GlobalExecutionId;
using RawRabbit.Enrichers.MessageContext;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Instantiation;
using RawRabbit.Messages.Sample;
using Serilog;

namespace RawRabbit.ConsoleApp.NetCore2.Sample
{
	public class Program
	{
		private static IBusClient _client;

		public static void Main(string[] args)
		{
			RunAsync().GetAwaiter().GetResult();
		}

		public static async Task RunAsync()
		{
			Log.Logger = new LoggerConfiguration()
				.WriteTo.LiterateConsole()
				.CreateLogger();

			_client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
			{
				ClientConfiguration = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("rawrabbit.json")
					.Build()
					.Get<RawRabbitConfiguration>(),
				Plugins = p => p
					.UseStackify()
					.UseGlobalExecutionId()
					.UseMessageContext<MessageContext>()
			});
			
			await _client.SubscribeAsync<StackifyTest, MessageContext>((requested, ctx) => StackifyTestHandler.Handle(requested));
			await _client.PublishAsync(new StackifyTest() { Value = "asdf" });
			
			await _client.SubscribeAsync<ValuesRequested, MessageContext>((requested, ctx) => ServerValuesAsync(requested, ctx));
			await _client.RespondAsync<ValueRequest, ValueResponse>(request => SendValuesThoughRpcAsync(request));
		}

		private static Task<ValueResponse> SendValuesThoughRpcAsync(ValueRequest request)
		{
			return Task.FromResult(new ValueResponse
			{
				Value = $"value{request.Value}"
			});
		}

		private static Task ServerValuesAsync(ValuesRequested message, MessageContext ctx)
		{
			var values = new List<string>();
			for (var i = 0; i < message.NumberOfValues; i++)
			{
				values.Add($"value{i}");
			}
			return _client.PublishAsync(new ValuesCalculated { Values = values });
		}
	}
}
