namespace RawRabbit.Client
{
	public class RawRabbitConfiguration
	{
		public string Hostname { get; set; }

		public static RawRabbitConfiguration Default = new RawRabbitConfiguration
		{
			Hostname = "localhost"
		};
	}
}
