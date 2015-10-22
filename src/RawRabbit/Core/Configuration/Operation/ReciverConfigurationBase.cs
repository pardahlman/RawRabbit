namespace RawRabbit.Core.Configuration.Operation
{
	public class ReciverConfigurationBase : ConfigurationBase
	{
		public bool NoAck { get; set; }
		public ushort PrefetchCount { get; set; }
	}
}
