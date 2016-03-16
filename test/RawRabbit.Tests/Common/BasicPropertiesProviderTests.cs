using Xunit;
using RawRabbit.Common;
using RawRabbit.Configuration;

namespace RawRabbit.Tests.Common
{
	public class BasicPropertiesProviderTests
	{
		[Fact]
		public void Should_Be_Able_To_Get_Type_Property_For_Basic_Type()
		{
			/* Setup */
			var provider = new BasicPropertiesProvider(new RawRabbitConfiguration());

			/* Test */
			var type = provider.GetProperties<First>().Headers[PropertyHeaders.MessageType];

			/* Assert */
			Assert.Equal(expected: "RawRabbit.Tests.Common.First, RawRabbit.Tests", actual: type);
		}

		[Fact]
		public void Should_Be_Able_To_Get_Type_Property_For_Type_With_Generic_Type_Arguments()
		{
			/* Setup */
			var provider = new BasicPropertiesProvider(new RawRabbitConfiguration());

			/* Test */
			var type = provider.GetProperties<Generic<First, Second>>().Headers[PropertyHeaders.MessageType];

			/* Assert */
			Assert.Equal(expected: "RawRabbit.Tests.Common.Generic`2[[RawRabbit.Tests.Common.First, RawRabbit.Tests],[RawRabbit.Tests.Common.Second, RawRabbit.Tests]], RawRabbit.Tests", actual: type);
		}

		private class Generic<TFirst, TSecond>
		{
			public TFirst First;
			public TSecond Second;
		}

		private class First
		{
		}

		private class Second
		{
		}

	}
}
