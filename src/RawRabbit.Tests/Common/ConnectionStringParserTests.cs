using System;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ConnectionStringParserTests
	{
		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString()
		{
			/* Setup */
			const string str = "brokers=firstUser:firstPassword@firstHost:1111/firstVhost,secondUser:secondPassword@secondHost:2222/secondVhost;requestTimeout=3600";
			var parser = new ConnectionStringParser();

			/* Test */
			var config = parser.Parse(str);

			/* Assert */
			Assert.Equal(config.RequestTimeout, TimeSpan.FromSeconds(3600));
			Assert.Equal(config.Brokers[0].Username, "firstUser");
			Assert.Equal(config.Brokers[0].Password, "firstPassword");
			Assert.Equal(config.Brokers[0].Hostname, "firstHost");
			Assert.Equal(config.Brokers[0].Port, 1111);
			Assert.Equal(config.Brokers[0].VirtualHost, "/firstVhost");
			Assert.Equal(config.Brokers[1].Username, "secondUser");
			Assert.Equal(config.Brokers[1].Password, "secondPassword");
			Assert.Equal(config.Brokers[1].Hostname, "secondHost");
			Assert.Equal(config.Brokers[1].Port, 2222);
			Assert.Equal(config.Brokers[1].VirtualHost, "/secondVhost");
		}
	}
}
