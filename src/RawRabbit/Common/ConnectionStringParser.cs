using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using RawRabbit.Configuration;

namespace RawRabbit.Common
{
	public class ConnectionStringParser
	{
		private static readonly Regex MainRegex = new Regex(@"((?<username>.*):(?<password>.*)@)?(?<hosts>[^\/\?:]*)(:(?<port>[^\/\?]*))?(?<vhost>\/[^\?]*)?(\?(?<parameters>.*))?");
		private static readonly Regex ParametersRegex = new Regex(@"(?<name>[^?=&]+)=(?<value>[^&]*)?");
		private static readonly RawRabbitConfiguration Defaults = RawRabbitConfiguration.Local;

		public static RawRabbitConfiguration Parse(string connectionString)
		{
			var mainMatch = MainRegex.Match(connectionString);
			var port = Defaults.Port;
			if (RegexMatchGroupIsNonEmpty(mainMatch, "port"))
			{
				var suppliedPort = mainMatch.Groups["port"].Value;
				if (!int.TryParse(suppliedPort, out port))
				{
					throw new FormatException($"The supplied port '{suppliedPort}' in the connection string is not a number");
				}
			}

			var cfg = new RawRabbitConfiguration
			{
				Username = RegexMatchGroupIsNonEmpty(mainMatch, "username") ? mainMatch.Groups["username"].Value : Defaults.Username,
				Password = RegexMatchGroupIsNonEmpty(mainMatch, "password") ? mainMatch.Groups["password"].Value : Defaults.Password,
				Hostnames = mainMatch.Groups["hosts"].Value.Split(',').ToList(),
				Port = port,
				VirtualHost = RegexMatchGroupIsNonEmpty(mainMatch, "vhost") ? mainMatch.Groups["vhost"].Value : Defaults.VirtualHost
			};

			var parametersMatches = ParametersRegex.Matches(mainMatch.Groups["parameters"].Value);
			foreach (Match match in parametersMatches)
			{
				var name = match.Groups["name"].Value.ToLower();
				var val = match.Groups["value"].Value.ToLower();
				var propertyInfo = cfg
					.GetType()
					.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

				if (propertyInfo == null)
				{
					throw new ArgumentException($"No configuration property named '{name}'");
				}

				if (propertyInfo.PropertyType == typeof (TimeSpan))
				{
					var convertedValue = TimeSpan.FromSeconds(int.Parse(val));
					propertyInfo.SetValue(cfg, convertedValue, null);
				}
				else
				{
					propertyInfo.SetValue(cfg, Convert.ChangeType(val, propertyInfo.PropertyType), null);
				}
			}

			return cfg;
		}

		private static bool RegexMatchGroupIsNonEmpty(Match match, string groupName)
		{
			return match.Groups[groupName].Success && match.Groups[groupName].Length > 0;
		}
	}
}
