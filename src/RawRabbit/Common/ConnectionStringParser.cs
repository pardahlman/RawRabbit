using System;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class ConnectionStringParser
	{
		private static readonly Regex MainRegex = new Regex(@"(?<username>.*):(?<password>.*)@(?<hosts>.*):(?<port>\d*)(?<vhost>\/[^\?]*)?(\?(?<parameters>.*))?");
		private static readonly Regex ParametersRegex = new Regex(@"(?<name>[^?=&]+)=(?<value>[^&]*)?");

		public static RawRabbitConfiguration Parse(string connectionString)
		{
			var mainMatch = MainRegex.Match(connectionString);
			var cfg = new RawRabbitConfiguration
			{
				Username = mainMatch.Groups["username"].Value,
				Password = mainMatch.Groups["password"].Value,
				VirtualHost = mainMatch.Groups["vhost"].Success ? mainMatch.Groups["vhost"].Value : "/",
				Port = int.Parse(mainMatch.Groups["port"].Value),
				Hostnames = mainMatch.Groups["hosts"].Value.Split(',').ToList()
			};

			var reqMatches = ParametersRegex.Matches(mainMatch.Groups["parameters"].Value);

			foreach (Match match in reqMatches)
			{
				var name = match.Groups["name"].Value.ToLower();
				var val = match.Groups["value"].Value.ToLower();
				var propertyInfo = cfg.GetType()
					.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

				switch (propertyInfo.PropertyType.FullName)
				{
					case "System.TimeSpan":
						var convertedValue = TimeSpan.FromSeconds(int.Parse(val));
						propertyInfo.SetValue(cfg, convertedValue, null);
						break;
					default:
						propertyInfo.SetValue(cfg, Convert.ChangeType(val, propertyInfo.PropertyType), null);
						break;
				}
			}

			return cfg;
		}
	}
}
