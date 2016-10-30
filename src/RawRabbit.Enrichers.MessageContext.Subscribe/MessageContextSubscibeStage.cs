namespace RawRabbit.Enrichers.MessageContext.Subscribe
{
	public enum MessageContextSubscibeStage
	{
		MessageRecieved,
		MessageDeserialized,
		MessageContextDeserialized,
		MessageContextEnhanced,
		HandlerInvoked
	}
}
