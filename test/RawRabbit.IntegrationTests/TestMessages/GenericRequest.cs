namespace RawRabbit.IntegrationTests.TestMessages
{
	public class GenericRequest<TFirst, TSecond>
	{ }

	public class GenericResponse<TFirst, TSecond>
	{
		public string Prop { get; set; }
	}

	public class GenericMessage<TProp>
	{
		public TProp Prop { get; set; }
	}
}
