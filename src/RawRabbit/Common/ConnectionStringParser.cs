using System;
using System.Text.RegularExpressions;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class ConnectionStringParser
	{
		private static readonly Regex _requestTimeout = new Regex(@"[Rr]equest[Tt]imeout=(?<timeout>\d+)");
		private static readonly Regex _brokers = new Regex(@"[Bb]rokers=(?<brokers>.*);");
		private static readonly Regex _broker = new Regex(@"(?<user>.*?):(?<password>.*?)@(?<host>.*?):(?<port>.*?)(?<vhost>\/.*)");

		public static RawRabbitConfiguration Parse(string connectionString)
		{
			var cfg = new RawRabbitConfiguration();

			var brokersMatch = _brokers.Match(connectionString);
			var brokerStrs = brokersMatch.Groups["brokers"].Value.Split(',');

			foreach (var broker in brokerStrs)
			{
				int port;
				var brokerMatch = _broker.Match(broker);
				var brokerCfg = new BrokerConfiguration
				{
					Hostname = brokerMatch.Groups["host"].Value,
					VirtualHost = brokerMatch.Groups["vhost"].Value,
					Port = int.TryParse(brokerMatch.Groups["port"].Value, out port) ? port : default(int),
					Username = brokerMatch.Groups["user"].Value,
					Password = brokerMatch.Groups["password"].Value,
				};
				cfg.Brokers.Add(brokerCfg);
			}

			var reqMatch = _requestTimeout.Match(connectionString);
			var timeoutGrp = reqMatch.Groups["timeout"];
			if (timeoutGrp.Success)
			{
				cfg.RequestTimeout = TimeSpan.FromSeconds(int.Parse(timeoutGrp.Value));
			}

			return cfg;
		}
	}
}
