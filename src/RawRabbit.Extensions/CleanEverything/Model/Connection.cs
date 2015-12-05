using System;

namespace RawRabbit.Extensions.CleanEverything.Model
{
	public class Connection : IRabbtMqEntity
	{
		public string Address { get; set; }
		public string Name { get; set; }
		public int Port { get; set; }
		public string User { get; set; }
		public string Vhost { get; set; }
	}
}