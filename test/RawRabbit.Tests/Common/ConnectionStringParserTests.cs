using System;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ConnectionStringParserTests
	{
		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_With_Multiple_Hosts()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2:1234/virtualHost?requestTimeout=20";
			
			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);
			
			/* Assert */
			Assert.Equal(config.Username, "username");
			Assert.Equal(config.Password, "password");
			Assert.Equal(config.VirtualHost, "/virtualHost");
			Assert.Equal(config.Hostnames[0], "host1");
			Assert.Equal(config.Hostnames[1], "host2");
			Assert.Equal(config.RequestTimeout, TimeSpan.FromSeconds(20));
		}
	}
}
