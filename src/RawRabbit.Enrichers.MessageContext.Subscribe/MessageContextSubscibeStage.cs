namespace RawRabbit.Enrichers.MessageContext.Subscribe
{
	public enum MessageContextSubscibeStage
	{
		MessageReceived,
		MessageDeserialized,
		MessageContextDeserialized,
		MessageContextEnhanced,
		HandlerInvoked
	}
}
