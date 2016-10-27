using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Attributes;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Messages.Sample;
using RawRabbit.vNext;

namespace RawRabbit.ConsoleApp.Sample
{
	public class Program
	{
		private static ILegacyBusClient _client;

		public static void Main(string[] args)
		{
			_client = BusClientFactory.CreateDefault(
				cfg => cfg
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("rawrabbit.json"),
				ioc => ioc
					.AddSingleton<IConfigurationEvaluator, AttributeConfigEvaluator>()
				);

			_client.SubscribeAsync<ValuesRequested>(ServeValuesAsync);
			_client.RespondAsync<ValueRequest, ValueResponse>(SendValuesThoughRpcAsync);
		}

		private static Task<ValueResponse> SendValuesThoughRpcAsync(ValueRequest request, MessageContext context)
		{
			return Task.FromResult(new ValueResponse
			{
				Value = $"value{request.Value}"
			});
		}

		private static Task ServeValuesAsync(ValuesRequested message, MessageContext context)
		{
			var values = new List<string>();
			for (var i = 0; i < message.NumberOfValues; i++)
			{
				values.Add($"value{i}");
			}
			return _client.PublishAsync(new ValuesCalculated {Values = values});
		}
	}
}
