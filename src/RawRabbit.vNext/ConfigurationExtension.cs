using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using RawRabbit.Configuration;

namespace RawRabbit.vNext
{
	public static class ConfigurationExtension
	{
		public static RawRabbitConfiguration ForRawRabbit(this IConfiguration config, string sectionName = "RawRabbit")
		{
			var section = config.GetSection(sectionName);
			if (!section.GetChildren().Any())
			{
				throw new ArgumentException($"Unable to configuration section '{sectionName}'. Make sure it exists in the provided configuration");
			}
			return section.Get<RawRabbitConfiguration>();
		}
	}
}
