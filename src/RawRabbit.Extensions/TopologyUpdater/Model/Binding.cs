using Newtonsoft.Json;

namespace RawRabbit.Extensions.TopologyUpdater.Model
{
	public class Binding
	{
		public string Source { get; set; }
		public string Vhost { get; set; }
		public string Destination { get; set; }
		[JsonProperty("destination_type")]
		public string DestinationType { get; set; }
		[JsonProperty("routing_key")]
		public string RoutingKey { get; set; }
		public object Arguments { get; set; }
		[JsonProperty("properties_key")]
		public string PropertiesKey { get; set; }
	}
}
