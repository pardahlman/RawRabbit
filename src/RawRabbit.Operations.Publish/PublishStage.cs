namespace RawRabbit.Operations.Publish
{
	public enum PublishStage
	{
		Initiated,
		ExchangeConfigured,
		RoutingKeyCreated,
		ExchangeDeclared,
		MessageSerialized,
		BasicPropertiesCreated,
		ChannelCreated,
		PreMessagePublish,
		MessagePublished
	}
}
