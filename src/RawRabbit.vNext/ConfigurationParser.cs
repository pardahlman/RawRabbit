using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using RawRabbit.Configuration;
using RawRabbit.Configuration.Exchange;

namespace RawRabbit.vNext
{
	public interface IConfigurationParser
	{
		RawRabbitConfiguration Parse(IConfiguration root);
	}

	public class ConfigurationParser : IConfigurationParser
	{
		public RawRabbitConfiguration Parse(IConfiguration root)
		{
			if (root == null)
			{
				throw new ArgumentNullException(nameof(root));
			}
			
			var cfg = new RawRabbitConfiguration();

			var timeout = root["Data:RawRabbit:RequestTimeout"];
			if (timeout != null)
			{
				cfg.RequestTimeout = ParseToTimeSpan(timeout);
			}
			
			bool autoDelete;
			if(bool.TryParse(root["Data:RawRabbit:Queue:AutoDelete"], out autoDelete))
			{
				cfg.Queue.AutoDelete = autoDelete;
			}
			bool durable;
			if (bool.TryParse(root["Data:RawRabbit:Queue:Durable"], out durable))
			{
				cfg.Queue.Durable = durable;
			}
			bool exclusive;
			if (bool.TryParse(root["Data:RawRabbit:Queue:Exclusive"], out exclusive))
			{
				cfg.Queue.Exclusive = exclusive;
			}
			bool xcAutoDelete;
			if (bool.TryParse(root["Data:RawRabbit:Exchange:AutoDelete"], out xcAutoDelete))
			{
				cfg.Exchange.AutoDelete= xcAutoDelete;
			}
			bool xcDurable;
			if (bool.TryParse(root["Data:RawRabbit:Exchange:Durable"], out xcDurable))
			{
				cfg.Exchange.Durable = xcDurable;
			}
			ExchangeType type;
			if (Enum.TryParse(root["Data:RawRabbit:Exchange:Type"], out type))
			{
				cfg.Exchange.Type = type;
			}

			cfg.Brokers = GetBrokers(root);
			return cfg;
		}

		private static List<BrokerConfiguration> GetBrokers(IConfiguration root)
		{
			var result = new List<BrokerConfiguration>();
			var continueParsing = true;
			var index = 0;
			while (continueParsing)
			{
				int port;
				var broker = new BrokerConfiguration
				{
					Hostname = root[$"Data:RawRabbit:Brokers:{index}:Hostname"],
					VirtualHost = root[$"Data:RawRabbit:Brokers:{index}:VirtualHost"],
					Port = int.TryParse(root[$"Data:RawRabbit:Brokers:{index}:Port"], out port) ? port : default(int),
					Username = root[$"Data:RawRabbit:Brokers:{index}:Username"],
					Password = root[$"Data:RawRabbit:Brokers:{index}:Password"]
				};
				if (string.IsNullOrWhiteSpace(broker.Hostname))
				{
					continueParsing = false;
					continue;
				}
				result.Add(broker);
				index++;
			}
			return result;
		}

		private static TimeSpan ParseToTimeSpan(string timeout)
		{
			var parts = timeout
				.Split(':')
				.Select(int.Parse)
				.ToList();

			if (parts.Count == 2)
			{
				return new TimeSpan(0,parts[0], parts[1]);
			}
			if (parts.Count == 3)
			{
				return new TimeSpan(parts[0], parts[1], parts[2]);
			}
			if (parts.Count == 4)
			{
				return new TimeSpan(parts[0], parts[1], parts[2], parts[3]);
			}
			throw new Exception("Unable to parse timeout");
		}
	}
}
