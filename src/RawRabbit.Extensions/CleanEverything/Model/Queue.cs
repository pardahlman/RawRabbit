namespace RawRabbit.Extensions.CleanEverything.Model
{
	public class Queue : IRabbtMqEntity
	{
		public bool AutoDelete { get; set; }
		public int Consumers { get; set; }
		public bool Durable { get; set; }
		public string Name { get; set; }
		public string Node { get; set; }
		public int Messages { get; set; }
		public string Vhost { get; set; }
	}
}