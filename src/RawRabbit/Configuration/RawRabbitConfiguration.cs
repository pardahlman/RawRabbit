using System.Collections.Generic;

namespace RawRabbit.Configuration
{
	public class RawRabbitConfiguration
	{
		public string Hostname { get; set; }
		public List<ConnectionConfiguration> Connection { get; set; }

		public static RawRabbitConfiguration Default = new RawRabbitConfiguration
		{
			Hostname = "localhost"
		};
	}

	public class ConnectionConfiguration
	{
		public string Hostname { get; set; }
		public string VirtualHost { get; set; }

		public static ConnectionConfiguration Default => new ConnectionConfiguration
		{
			Hostname = "localhost",
			VirtualHost = ""
		};
	}
}
