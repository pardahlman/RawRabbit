using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Extensions.CleanEverything.Configuration;
using RawRabbit.Extensions.CleanEverything.Http;
using RawRabbit.Extensions.CleanEverything.Model;
using RawRabbit.Extensions.Client;

namespace RawRabbit.Extensions.CleanEverything
{
	public static class CleanEverythingExtension
	{
		public static Task CleanAsync<TMessageContext>(this IBusClient<TMessageContext> busClient, Action<ICleanConfigurationBuilder> cfg = null)
			where TMessageContext : IMessageContext
		{
			var extended = (busClient as ExtendableBusClient<TMessageContext>);
			if (extended == null)
			{
				throw new InvalidOperationException("Bus client does not support extensions. Make sure that the client is of type ExtendableBusClient.");
			}

			var brokerConfig = extended.GetService<RawRabbitConfiguration>().Brokers;
			if (!brokerConfig.Any())
			{
				brokerConfig.Add(BrokerConfiguration.Local);
			}

			var config = GetCleanConfiguration(cfg);
			Task removeQueueTask = Task.FromResult(true);
			Task removeExchangesTask = Task.FromResult(true);
			Task closeConnectionsTask = Task.FromResult(true);
			if (config.RemoveQueues)
			{
				removeQueueTask = RemoveEntities<Exchange>("queues", brokerConfig);
			}
			if (config.RemoveExchanges)
			{
				removeExchangesTask = RemoveEntities<Exchange>("exchanges", brokerConfig);
			}
			if (config.CloseConnections)
			{
				throw new NotImplementedException("Removal of connection is not implemented.");
				closeConnectionsTask = RemoveEntities<Connection>("connections", brokerConfig);
			}
			return Task.WhenAll(removeQueueTask, removeExchangesTask, closeConnectionsTask);
		}

		private static Task RemoveEntities<TEntity>(string entityName, IEnumerable<BrokerConfiguration> brokerConfig) where TEntity : IRabbtMqEntity
		{
			var tasks = new List<Task>();
			foreach (var cfg in brokerConfig)
			{
				var credentials = new NetworkCredential(cfg.Username, cfg.Password);
				var queuesTask = new WebRequester()
					.WithUrl($"http://{cfg.Hostname}:15672/api/{entityName}")
					.WithMethod(HttpMethod.Get)
					.WithCredentials(credentials)
					.PerformAsync<List<TEntity>>()
					.ContinueWith(entitiesTask =>
					{
						var removeTask = new List<Task>();
						foreach (var entity in entitiesTask.Result)
						{
							if (string.IsNullOrEmpty(entity.Name) ||entity.Name.StartsWith("amq."))
							{
								continue;
							}
							var removeEntityTask = new WebRequester()
								.WithUrl($"http://{cfg.Hostname}:15672/api/{entityName}/{Uri.EscapeDataString(entity.Vhost)}/{Uri.EscapeUriString(entity.Name)}")
								.WithMethod(HttpMethod.Delete)
								.WithCredentials(credentials)
								.GetResponseAsync();
							removeTask.Add(removeEntityTask);
						}
						return Task.WhenAll(removeTask);
					});
				tasks.Add(queuesTask);
			}
			return Task.WhenAll(tasks);

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
