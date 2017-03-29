using System;
using BenchmarkDotNet.Running;
using Xunit;

namespace RawRabbit.PerformanceTest
{
	public class Harness
	{
		[Fact]
		public void PublishAcknowledge()
		{
			var result = BenchmarkRunner.Run<DeliveryModePerf>();
			Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
		}
	}
}
