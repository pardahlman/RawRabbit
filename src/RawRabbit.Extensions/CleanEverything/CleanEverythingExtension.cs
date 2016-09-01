using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Extensions.CleanEverything.Configuration;
using RawRabbit.Extensions.CleanEverything.Model;

namespace RawRabbit.Extensions.CleanEverything
{
	public static class CleanEverythingExtension
	{
		public static Task CleanAsync<TMessageContext>(this IBusClient<TMessageContext> busClient, Action<ICleanConfigurationBuilder> cfg = null)
			where TMessageContext : IMessageContext
		{
			var extended = (busClient as Client.IBusClient<TMessageContext>);
			if (extended == null)
			{
				throw new InvalidOperationException("Bus client does not support extensions. Make sure that the client is of type ExtendableBusClient.");
			}
			var rawConfig = extended.GetService<RawRabbitConfiguration>();
			var config = GetCleanConfiguration(cfg);
			Task removeQueueTask = Task.FromResult(true);
			Task removeExchangesTask = Task.FromResult(true);
			Task closeConnectionsTask = Task.FromResult(true);
			if (config.RemoveQueues)
			{
				removeQueueTask = RemoveEntities<Exchange>("queues", rawConfig);
			}
			if (config.RemoveExchanges)
			{
				removeExchangesTask = RemoveEntities<Exchange>("exchanges", rawConfig);
			}
			if (config.CloseConnections)
			{
				throw new NotImplementedException("Removal of connection is not implemented.");
			}
			return Task.WhenAll(removeQueueTask, removeExchangesTask, closeConnectionsTask);
		}

		private static async Task RemoveEntities<TEntity>(string entityName, RawRabbitConfiguration config) where TEntity : IRabbtMqEntity
		{
			var tasks = new List<Task>();

			using (var handler = new HttpClientHandler { Credentials = new NetworkCredential(config.Username, config.Password) })
			using (var httpClient = new HttpClient(handler))
			{
				foreach (var hostname in config.Hostnames)
				{
					var response = await httpClient.GetAsync($"http://{hostname}:15672/api/{entityName}");
					var entityStr = await response.Content.ReadAsStringAsync();
					var entites = JsonConvert.DeserializeObject<List<TEntity>>(entityStr);
					foreach (var entity in entites)
					{
						if (string.IsNullOrEmpty(entity.Name) || entity.Name.StartsWith("amq."))
						{
							continue;
						}
						var removeEntityTask = httpClient
							.DeleteAsync(new Uri($"http://{hostname}:15672/api/{entityName}/{Uri.EscapeDataString(entity.Vhost)}/{Uri.EscapeUriString(entity.Name)}"));

						tasks.Add(removeEntityTask);
					}
				}
			}
			await Task.WhenAll(tasks);
		}


		private static CleanConfiguration GetCleanConfiguration(Action<ICleanConfigurationBuilder> cfg)
		{
			if (cfg == null)
			{
				return CleanConfiguration.RemoveAll;
			}
			var builder = new CleanConfigurationBuilder();
			cfg(builder);
			return builder.Configuration;
		}
	}
}
