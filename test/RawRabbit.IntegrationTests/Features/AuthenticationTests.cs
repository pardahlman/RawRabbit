﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
    public class AuthenticationTests
    {
        [Fact]
        public async Task Should_Give_Clear_Error_Message_If_User_Does_Not_Exist()
        {
            /* Setup */
            var config = new RawRabbitConfiguration
            {
                Username = "RawRabbit",
                Password = "wrong-pass",
                Hostnames = {"localhost"},
                VirtualHost = "/",
                Port = 5672
            };

            /* Test */
            /* Assert */
            Assert.ThrowsAny<AuthenticationFailureException>(() => TestClientFactory.CreateNormal(ioc => ioc.AddSingleton(p => config)));
        }

        [Fact]
        public async Task Should_Use_Guest_Credentials_By_Default()
        {
            /* Setup */
            /* Test */
            var b = TestClientFactory.CreateNormal();
            b.Dispose();

            /* Assert */
            Assert.True(true, "Does not throw.");
        }

        [Fact]
        public async Task Should_Not_Throw_If_Credentials_Are_Correct()
        {
            /* Setup */
            var config = new RawRabbitConfiguration
            {
                Username = "RawRabbit",
                Password = "RawRabbit",
                Hostnames = new List<string> {"localhost"},
                VirtualHost = "/",
                Port = 5672
            };

            /* Test */
            var b = TestClientFactory.CreateNormal(ioc => ioc.AddSingleton(p => config));
            b.Dispose();

            /* Assert */
            Assert.True(true, "Does not throw.");
        }
    }
}
