using System;
using BenchmarkDotNet.Running;
using Xunit;

namespace RawRabbit.PerformanceTest
{
	public class Harness
	{
		[Fact]
		public void PubSubBenchmarks()
		{
			var result = BenchmarkRunner.Run<PubSubBenchmarks>();
			Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
		}

		[Fact]
		public void RpcBenchmarks()
		{
			var result = BenchmarkRunner.Run<RpcBenchmarks>();
			Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
		}

		[Fact]
		public void MessageContextBenchmarks()
		{
			var result = BenchmarkRunner.Run<MessageContextBenchmarks>();
			Assert.NotEqual(TimeSpan.Zero, result.TotalTime);
		}
	}
}
