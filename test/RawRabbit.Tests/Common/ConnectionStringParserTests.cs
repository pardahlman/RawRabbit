using System;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ConnectionStringParserTests
	{
		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Virtual_Host()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2:1234";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("username", config.Username);
			Assert.Equal("password", config.Password);
			Assert.Equal("/", config.VirtualHost);
			Assert.Equal("host1", config.Hostnames[0]);
			Assert.Equal("host2", config.Hostnames[1]);
			Assert.Equal(1234, config.Port);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_With_Parameters()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2:1234/virtualHost?" +
											"requestTimeout=10" +
											"&publishConfirmTimeout=20" +
											"&recoveryInterval=30" +
											"&autoCloseConnection=false" +
											"&persistentDeliveryMode=false" +
											"&automaticRecovery=false" +
											"&topologyRecovery=false";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("username", config.Username);
			Assert.Equal("password", config.Password);
			Assert.Equal("/virtualHost", config.VirtualHost);
			Assert.Equal("host1", config.Hostnames[0]);
			Assert.Equal("host2", config.Hostnames[1]);
			Assert.Equal(1234, config.Port);
			Assert.Equal(TimeSpan.FromSeconds(10), config.RequestTimeout);
			Assert.Equal(TimeSpan.FromSeconds(20), config.PublishConfirmTimeout);
			Assert.Equal(TimeSpan.FromSeconds(30), config.RecoveryInterval);
			Assert.Equal(false, config.AutoCloseConnection);
			Assert.Equal(false, config.PersistentDeliveryMode);
			Assert.Equal(false, config.AutomaticRecovery);
			Assert.Equal(false, config.TopologyRecovery);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Virtual_Host_And_With_Parameters()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2:1234?" +
											"requestTimeout=10" +
											"&publishConfirmTimeout=20" +
											"&recoveryInterval=30" +
											"&autoCloseConnection=false" +
											"&persistentDeliveryMode=false" +
											"&automaticRecovery=false" +
											"&topologyRecovery=false";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("username", config.Username);
			Assert.Equal("password", config.Password);
			Assert.Equal("/", config.VirtualHost);
			Assert.Equal("host1", config.Hostnames[0]);
			Assert.Equal("host2", config.Hostnames[1]);
			Assert.Equal(1234, config.Port);
			Assert.Equal(TimeSpan.FromSeconds(10), config.RequestTimeout);
			Assert.Equal(TimeSpan.FromSeconds(20), config.PublishConfirmTimeout);
			Assert.Equal(TimeSpan.FromSeconds(30), config.RecoveryInterval);
			Assert.Equal(false, config.AutoCloseConnection);
			Assert.Equal(false, config.PersistentDeliveryMode);
			Assert.Equal(false, config.AutomaticRecovery);
			Assert.Equal(false, config.TopologyRecovery);
		}

	}
}
