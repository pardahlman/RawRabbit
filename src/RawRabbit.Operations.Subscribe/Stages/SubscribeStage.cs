namespace RawRabbit.Operations.Subscribe.Stages
{
	public enum SubscribeStage
	{
		ConsumeConfigured,
		QueueDeclared,
		ExchangeDeclared,
		QueueBound,
		ConsumerChannelCreated,
		ConsumerCreated
	}
}
