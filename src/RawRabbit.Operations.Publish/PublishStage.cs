namespace RawRabbit.Operations.Publish
{
	public enum PublishStage
	{
		Initiated,
		PublishConfigured,
		RoutingKeyCreated,
		ExchangeDeclared,
		MessageSerialized,
		BasicPropertiesCreated,
		ChannelCreated,
		PreMessagePublish,
		MessagePublished
	}
}
