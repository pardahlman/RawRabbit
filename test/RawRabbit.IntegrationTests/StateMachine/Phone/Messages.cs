using System;

namespace RawRabbit.IntegrationTests.StateMachine.Phone
{
	public class PhoneCallDialed {
		public Guid CallId { get; set; }
	}
	public class PhoneRinging {
		public Guid CallId { get; set; }
	}
	public class PhonePickedUp {
		public Guid CallId { get; set; }
	}

	public class DialPhoneNumber
	{
		public string Number { get; set; }
	}
	public class DialSignalSent {
		public Guid CallId { get; set; }
	}
}
