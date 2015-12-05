namespace RawRabbit.Extensions.CleanEverything.Model
{
	public class Exchange : IRabbtMqEntity
	{
		public string Name { get; set; }
		public string Vhost { get; set; }
		public string Type { get; set; }
		public bool Durable { get; set; }
		public bool AutoDelete { get; set; }
		public bool Internal { get; set; }
	}
}