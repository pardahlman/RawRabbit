namespace RawRabbit.Operations.Publish
{
	public enum PublishStage
	{
		ExchangeConfigured,
		RoutingKeyCreated,
		ExchangeDeclared,
		MessageSerialized,
		BasicPropertiesCreated,
		ChannelCreated,
		MessagePublished
	}
}
