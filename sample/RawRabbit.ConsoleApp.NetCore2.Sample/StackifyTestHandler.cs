using System.Threading.Tasks;

using RawRabbit.Messages.Sample;

namespace RawRabbit.ConsoleApp.NetCore2.Sample
{
	public static class StackifyTestHandler
	{
		public static Task<StackifyTest> Handle(StackifyTest testData)
		{
			return Task.FromResult(new StackifyTest
			{
				Value = $"value{testData.Value}"
			});
		}
	}
}
