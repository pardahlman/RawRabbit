namespace RawRabbit.Operations.Subscribe.Stages
{
	public enum SubscribeStage
	{
		ConfigurationCreated,
		QueueDeclared,
		ExchangeDeclared,
		QueueBound,
		ConsumerChannelCreated,
		ConsumerCreated
	}
}
