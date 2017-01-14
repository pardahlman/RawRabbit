using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RawRabbit.Messages.Sample;
using RawRabbit.vNext;
using RawRabbit.vNext.Pipe;

namespace RawRabbit.ConsoleApp.Sample
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
			_client = RawRabbitFactory.CreateSingleton(new RawRabbitOptions
			{
				Configuration = cfg => cfg
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("rawrabbit.json")
			});

			await _client.SubscribeAsync<ValuesRequested>(requested => ServeValuesAsync(requested));
			await _client.RespondAsync<ValueRequest, ValueResponse>(request => SendValuesThoughRpcAsync(request));
		}

		private static Task<ValueResponse> SendValuesThoughRpcAsync(ValueRequest request)
		{
			return Task.FromResult(new ValueResponse
			{
				Value = $"value{request.Value}"
			});
		}

		private static Task ServeValuesAsync(ValuesRequested message)
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
