using System;
using System.Text.RegularExpressions;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class ConnectionStringParser
	{
		private readonly Regex _requestTimeout = new Regex(@"[Rr]equest[Tt]imeout=(?<timeout>\d+)");
		private readonly Regex _brokers = new Regex(@"[Bb]rokers=(?<brokers>.*);");
		private readonly Regex _broker = new Regex(@"(?<user>.*?):(?<password>.*?)@(?<host>.*?):(?<port>.*?)(?<vhost>\/.*)");

		public RawRabbitConfiguration Parse(string connectionString)
		{
			var cfg = new RawRabbitConfiguration();

			var brokersMatch = _brokers.Match(connectionString);
			var brokerStrs = brokersMatch.Groups["brokers"].Value.Split(',');

			foreach (var broker in brokerStrs)
			{
				var brokerMatch = _broker.Match(broker);
				var brokerCfg = new BrokerConfiguration
				{
					Hostname = brokerMatch.Groups["host"].Value,
					VirtualHost = brokerMatch.Groups["vhost"].Value,
					Port = brokerMatch.Groups["port"].Value,
					Username = brokerMatch.Groups["user"].Value,
					Password = brokerMatch.Groups["password"].Value,
				};
				cfg.Brokers.Add(brokerCfg);
			}

			var reqMatch = _requestTimeout.Match(connectionString);
			var timeoutGrp = reqMatch.Groups["timeout"];
			if (timeoutGrp != null)
			{
				cfg.RequestTimeout = TimeSpan.FromSeconds(int.Parse(timeoutGrp.Value));
			}

			return cfg;
		}
	}
}
