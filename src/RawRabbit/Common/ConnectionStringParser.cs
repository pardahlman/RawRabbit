using System;
using System.Linq;
using System.Text.RegularExpressions;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class ConnectionStringParser
	{
		private static readonly Regex MainRegex = new Regex(@"(?<username>.*):(?<password>.*)@(?<hosts>.*):(?<port>\d*)(?<vhost>.*)\?");
		private static readonly Regex RequestTimeout = new Regex(@"[Rr]equest[Tt]imeout=(?<timeout>\d+)");

		public static RawRabbitConfiguration Parse(string connectionString)
		{
			var mainMatch = MainRegex.Match(connectionString);
			var cfg = new RawRabbitConfiguration
			{
				Username = mainMatch.Groups["username"].Value,
				Password = mainMatch.Groups["password"].Value,
				VirtualHost = mainMatch.Groups["vhost"].Value,
				Port = int.Parse(mainMatch.Groups["port"].Value),
				Hostnames = mainMatch.Groups["hosts"].Value.Split(',').ToList()
			};

			var reqMatch = RequestTimeout.Match(connectionString);
			var timeoutGrp = reqMatch.Groups["timeout"];
			if (timeoutGrp.Success)
			{
				cfg.RequestTimeout = TimeSpan.FromSeconds(int.Parse(timeoutGrp.Value));
			}

			return cfg;
		}
	}
}
