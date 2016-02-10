using System;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ConnectionStringParserTests
	{
		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Parameters()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2:1234/virtualHost";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("username", config.Username);
			Assert.Equal("password", config.Password);
			Assert.Equal("/virtualHost", config.VirtualHost);
			Assert.Equal("host1", config.Hostnames[0]);
			Assert.Equal("host2", config.Hostnames[1]);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Parameters_And_Virtual_Host()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2:1234";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("username", config.Username);
			Assert.Equal("password", config.Password);
			Assert.Equal("host1", config.Hostnames[0]);
			Assert.Equal("host2", config.Hostnames[1]);
		}

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
