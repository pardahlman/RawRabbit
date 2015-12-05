namespace RawRabbit.Extensions.CleanEverything.Configuration
{
	public class CleanConfiguration
	{
		public bool RemoveQueues { get; set; }
		public bool RemoveExchanges { get; set; }
		public bool CloseConnections { get; set; }

		public static CleanConfiguration RemoveAll => new CleanConfiguration
		{
			CloseConnections = true,
			RemoveExchanges = true,
			RemoveQueues = true
		};
	}
}
