namespace RawRabbit.Operations.Publish
{
	public enum PublishStage
	{
		Initiated,
		PublishConfigured,
		ExchangeDeclared,
		MessageSerialized,
		BasicPropertiesCreated,
		ChannelCreated,
		PreMessagePublish,
		MessagePublished
	}
}
