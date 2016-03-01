namespace RawRabbit.IntegrationTests.TestMessages
{
	public class First { }
	public class Second { }
	public class GenericRequest<TFirst, TSecond>
	{
	}

	public class GenericResponse<TFirst, TSecond>
	{
		public string Prop { get; set; }
	}
}
