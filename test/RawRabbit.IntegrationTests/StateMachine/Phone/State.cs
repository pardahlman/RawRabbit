namespace RawRabbit.IntegrationTests.StateMachine.Phone
{
	public enum State
	{
		OnHook,
		OffHook,
		DialTone,
		Connecting,
		Ringing,
		Connected,
		OnHold,
		PhoneDestroyed
	}
}