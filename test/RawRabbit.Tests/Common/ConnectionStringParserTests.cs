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
            Assert.Equal(expected: "guest", actual: config.Username);
            Assert.Equal(expected: "guest", actual: config.Password);
            Assert.Equal(expected: "/", actual: config.VirtualHost);
            Assert.Equal(expected: "host", actual: config.Hostnames[0]);
            Assert.Equal(expected: 5672, actual: config.Port);
        }

        [Fact]
        public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials_With_Port()
        {
            /* Setup */
            const string connectionString = "host:1234";

            /* Test */
            var config = ConnectionStringParser.Parse(connectionString);

            /* Assert */
            Assert.Equal(expected: "guest", actual: config.Username);
            Assert.Equal(expected: "guest", actual: config.Password);
            Assert.Equal(expected: "/", actual: config.VirtualHost);
            Assert.Equal(expected: "host", actual: config.Hostnames[0]);
            Assert.Equal(expected: 1234, actual: config.Port);
        }

        [Fact]
        public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials_With_VirtualHost()
        {
            /* Setup */
            const string connectionString = "host/virtualHost";

            /* Test */
            var config = ConnectionStringParser.Parse(connectionString);

            /* Assert */
            Assert.Equal(expected: "guest", actual: config.Username);
            Assert.Equal(expected: "guest", actual: config.Password);
            Assert.Equal(expected: "virtualHost", actual: config.VirtualHost);
            Assert.Equal(expected: "host", actual: config.Hostnames[0]);
            Assert.Equal(expected: 5672, actual: config.Port);
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
            Assert.Equal(expected: "guest", actual: config.Username);
            Assert.Equal(expected: "guest", actual: config.Password);
            Assert.Equal(expected: "/", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 5672, actual: config.Port);
            Assert.Equal(expected: TimeSpan.FromSeconds(10), actual: config.RequestTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(20), actual: config.PublishConfirmTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(30), actual: config.RecoveryInterval);
            Assert.Equal(expected: false, actual: config.AutoCloseConnection);
            Assert.Equal(expected: false, actual: config.PersistentDeliveryMode);
            Assert.Equal(expected: false, actual: config.AutomaticRecovery);
            Assert.Equal(expected: false, actual: config.TopologyRecovery);
        }

        [Fact]
        public void Should_Be_Able_To_Parse_ConnectionString_Without_Credentials_With_Port_And_VirtualHost()
        {
            /* Setup */
            const string connectionString = "host:1234/virtualHost";

            /* Test */
            var config = ConnectionStringParser.Parse(connectionString);

            /* Assert */
            Assert.Equal(expected: "guest", actual: config.Username);
            Assert.Equal(expected: "guest", actual: config.Password);
            Assert.Equal(expected: "virtualHost", actual: config.VirtualHost);
            Assert.Equal(expected: "host", actual: config.Hostnames[0]);
            Assert.Equal(expected: 1234, actual: config.Port);
        }

        [Fact]
        public void Should_Be_Able_To_Parse_ConnectionString_Without_Port()
        {
            /* Setup */
            const string connectionString = "username:password@host1,host2";

            /* Test */
            var config = ConnectionStringParser.Parse(connectionString);

            /* Assert */
            Assert.Equal(expected: "username", actual: config.Username);
            Assert.Equal(expected: "password", actual: config.Password);
            Assert.Equal(expected: "/", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 5672, actual: config.Port);
        }

        [Fact]
        public void Should_Be_Able_To_Parse_ConnectionString_Without_Port_With_VirtualHost()
        {
            /* Setup */
            const string connectionString = "username:password@host1,host2/virtualHost";

            /* Test */
            var config = ConnectionStringParser.Parse(connectionString);

            /* Assert */
            Assert.Equal(expected: "username", actual: config.Username);
            Assert.Equal(expected: "password", actual: config.Password);
            Assert.Equal(expected: "virtualHost", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 5672, actual: config.Port);
        }

        [Fact]
        public void Should_Be_Able_To_Parse_ConnectionString_With_Port_Without_VirtualHost()
        {
            /* Setup */
            const string connectionString = "username:password@host1,host2:1234";

            /* Test */
            var config = ConnectionStringParser.Parse(connectionString);

            /* Assert */
            Assert.Equal(expected: "username", actual: config.Username);
            Assert.Equal(expected: "password", actual: config.Password);
            Assert.Equal(expected: "/", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 1234, actual: config.Port);
        }

        [Fact]
        public void Should_Be_Able_To_Parse_ConnectionString_With_Port_And_VirtualHost()
        {
            /* Setup */
            const string connectionString = "username:password@host1,host2:1234/virtualHost";

            /* Test */
            var config = ConnectionStringParser.Parse(connectionString);

            /* Assert */
            Assert.Equal(expected: "username", actual: config.Username);
            Assert.Equal(expected: "password", actual: config.Password);
            Assert.Equal(expected: "virtualHost", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 1234, actual: config.Port);
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
            Assert.Equal(expected: "username", actual: config.Username);
            Assert.Equal(expected: "password", actual: config.Password);
            Assert.Equal(expected: "virtualHost", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 1234, actual: config.Port);
            Assert.Equal(expected: TimeSpan.FromSeconds(10), actual: config.RequestTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(20), actual: config.PublishConfirmTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(30), actual: config.RecoveryInterval);
            Assert.Equal(expected: false, actual: config.AutoCloseConnection);
            Assert.Equal(expected: false, actual: config.PersistentDeliveryMode);
            Assert.Equal(expected: false, actual: config.AutomaticRecovery);
            Assert.Equal(expected: false, actual: config.TopologyRecovery);
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
            Assert.Equal(expected: "username", actual: config.Username);
            Assert.Equal(expected: "password", actual: config.Password);
            Assert.Equal(expected: "/", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 1234, actual: config.Port);
            Assert.Equal(expected: TimeSpan.FromSeconds(10), actual: config.RequestTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(20), actual: config.PublishConfirmTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(30), actual: config.RecoveryInterval);
            Assert.Equal(expected: false, actual: config.AutoCloseConnection);
            Assert.Equal(expected: false, actual: config.PersistentDeliveryMode);
            Assert.Equal(expected: false, actual: config.AutomaticRecovery);
            Assert.Equal(expected: false, actual: config.TopologyRecovery);
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
            Assert.Equal(expected: "guest", actual: config.Username);
            Assert.Equal(expected: "guest", actual: config.Password);
            Assert.Equal(expected: "virtualHost", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 1234, actual: config.Port);
            Assert.Equal(expected: TimeSpan.FromSeconds(10), actual: config.RequestTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(20), actual: config.PublishConfirmTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(30), actual: config.RecoveryInterval);
            Assert.Equal(expected: false, actual: config.AutoCloseConnection);
            Assert.Equal(expected: false, actual: config.PersistentDeliveryMode);
            Assert.Equal(expected: false, actual: config.AutomaticRecovery);
            Assert.Equal(expected: false, actual: config.TopologyRecovery);
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
            Assert.Equal(expected: "username", actual: config.Username);
            Assert.Equal(expected: "password", actual: config.Password);
            Assert.Equal(expected: "/", actual: config.VirtualHost);
            Assert.Equal(expected: "host1", actual: config.Hostnames[0]);
            Assert.Equal(expected: "host2", actual: config.Hostnames[1]);
            Assert.Equal(expected: 5672, actual: config.Port);
            Assert.Equal(expected: TimeSpan.FromSeconds(10), actual: config.RequestTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(20), actual: config.PublishConfirmTimeout);
            Assert.Equal(expected: TimeSpan.FromSeconds(30), actual: config.RecoveryInterval);
            Assert.Equal(expected: false, actual: config.AutoCloseConnection);
            Assert.Equal(expected: false, actual: config.PersistentDeliveryMode);
            Assert.Equal(expected: false, actual: config.AutomaticRecovery);
            Assert.Equal(expected: false, actual: config.TopologyRecovery);
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
            Assert.Equal(expected: "The supplied port 'port' in the connection string is not a number", actual: exception.Message);
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
            Assert.Equal(expected: "No configuration property named 'badproperty'", actual: exception.Message);
        }

    }
}
