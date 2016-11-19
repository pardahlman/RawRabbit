namespace RawRabbit.IntegrationTests.StateMachine.Phone
{
	public class PhoneCallDialed { }
	public class PhoneRinging { }
	public class PhonePickedUp {}

	public class DialPhoneNumber
	{
		public string Number { get; set; }
	}
	public class DialSignalSent { }
}
