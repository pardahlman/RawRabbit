using System;
using RawRabbit.Common;
using Xunit;

namespace RawRabbit.Tests.Common
{
	public class ConnectionStringParserTests
	{
		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials()
		{
			/* Setup */
			const string connectionString = "host";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("guest", config.Username);
			Assert.Equal("guest", config.Password);
			Assert.Equal("/", config.VirtualHost);
			Assert.Equal("host", config.Hostnames[0]);
			Assert.Equal(5672, config.Port);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials_With_Port()
		{
			/* Setup */
			const string connectionString = "host:1234";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("guest", config.Username);
			Assert.Equal("guest", config.Password);
			Assert.Equal("/", config.VirtualHost);
			Assert.Equal("host", config.Hostnames[0]);
			Assert.Equal(1234, config.Port);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials_With_VirtualHost()
		{
			/* Setup */
			const string connectionString = "host/virtualHost";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("guest", config.Username);
			Assert.Equal("guest", config.Password);
			Assert.Equal("/virtualHost", config.VirtualHost);
			Assert.Equal("host", config.Hostnames[0]);
			Assert.Equal(5672, config.Port);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials_With_Parameters()
		{
			/* Setup */
			const string connectionString = "host1,host2?" +
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
			Assert.Equal("guest", config.Username);
			Assert.Equal("guest", config.Password);
			Assert.Equal("/", config.VirtualHost);
			Assert.Equal("host1", config.Hostnames[0]);
			Assert.Equal("host2", config.Hostnames[1]);
			Assert.Equal(5672, config.Port);
			Assert.Equal(TimeSpan.FromSeconds(10), config.RequestTimeout);
			Assert.Equal(TimeSpan.FromSeconds(20), config.PublishConfirmTimeout);
			Assert.Equal(TimeSpan.FromSeconds(30), config.RecoveryInterval);
			Assert.Equal(false, config.AutoCloseConnection);
			Assert.Equal(false, config.PersistentDeliveryMode);
			Assert.Equal(false, config.AutomaticRecovery);
			Assert.Equal(false, config.TopologyRecovery);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials_With_Port_And_VirtualHost()
		{
			/* Setup */
			const string connectionString = "host:1234/virtualHost";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("guest", config.Username);
			Assert.Equal("guest", config.Password);
			Assert.Equal("/virtualHost", config.VirtualHost);
			Assert.Equal("host", config.Hostnames[0]);
			Assert.Equal(1234, config.Port);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Port()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("username", config.Username);
			Assert.Equal("password", config.Password);
			Assert.Equal("/", config.VirtualHost);
			Assert.Equal("host1", config.Hostnames[0]);
			Assert.Equal("host2", config.Hostnames[1]);
			Assert.Equal(5672, config.Port);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Port_With_VirtualHost()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2/virtualHost";

			/* Test */
			var config = ConnectionStringParser.Parse(connectionString);

			/* Assert */
			Assert.Equal("username", config.Username);
			Assert.Equal("password", config.Password);
			Assert.Equal("/virtualHost", config.VirtualHost);
			Assert.Equal("host1", config.Hostnames[0]);
			Assert.Equal("host2", config.Hostnames[1]);
			Assert.Equal(5672, config.Port);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_With_Port_Without_VirtualHost()
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
		public void Should_Be_Able_To_Parse_ConnectionString_With_Port_And_VirtualHost()
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
			Assert.Equal(1234, config.Port);
		}

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_With_All_Attributes_And_Parameters()
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
		public void Should_Be_Able_To_Parse_ConnectionString_Without_VirtualHost_With_Parameters()
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

		[Fact]
		public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials_With_Port_And_VirtualHost_And_Parameters()
		{
			/* Setup */
			const string connectionString = "host1,host2:1234/virtualHost?" +
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
			Assert.Equal("guest", config.Username);
			Assert.Equal("guest", config.Password);
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
		public void Should_Be_Able_To_Parse_ConnectionString_Without_VirtualHost_And_Port_With_Parameters()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2?" +
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
			Assert.Equal(5672, config.Port);
			Assert.Equal(TimeSpan.FromSeconds(10), config.RequestTimeout);
			Assert.Equal(TimeSpan.FromSeconds(20), config.PublishConfirmTimeout);
			Assert.Equal(TimeSpan.FromSeconds(30), config.RecoveryInterval);
			Assert.Equal(false, config.AutoCloseConnection);
			Assert.Equal(false, config.PersistentDeliveryMode);
			Assert.Equal(false, config.AutomaticRecovery);
			Assert.Equal(false, config.TopologyRecovery);
		}

		[Fact]
		public void Should_Throw_Format_Exception_When_ConnectionString_Has_Bad_Port()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2:port";
			Exception exception = null;

			/* Test */
			try
			{
				ConnectionStringParser.Parse(connectionString);
			}
			catch (Exception e)
			{
				exception = e;
			}

			/* Assert */
			Assert.NotNull(exception);
			Assert.IsType(typeof(FormatException), exception);
			Assert.Equal("The supplied port 'port' in the connection string is not a number", exception.Message);
		}

		[Fact]
		public void Should_Throw_Argument_Exception_When_ConnectionString_Has_Bad_Property()
		{
			/* Setup */
			const string connectionString = "username:password@host1,host2?badproperty=true";
			Exception exception = null;

			/* Test */
			try
			{
				ConnectionStringParser.Parse(connectionString);
			}
			catch (Exception e)
			{
				exception = e;
			}

			/* Assert */
			Assert.NotNull(exception);
			Assert.IsType(typeof(ArgumentException), exception);
			Assert.Equal("No configuration property named 'badproperty'", exception.Message);
		}

	}
}
