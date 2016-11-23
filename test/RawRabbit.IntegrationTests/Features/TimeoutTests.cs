using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Common;
using RawRabbit.Configuration;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.Features
{
    public class TimeoutTests
    {
        [Fact]
        public async Task Should_Interupt_Task_After_Timeout_Not_Met()
        {
            /* Setup */
            var requester = TestClientFactory.CreateNormal(ioc =>
            {
                ioc.AddSingleton(p =>
                {
                    var cfg = RawRabbitConfiguration.Local;
                    cfg.RequestTimeout = TimeSpan.FromMilliseconds(200);
                    return cfg;
                });
            });
            using (var responder = TestClientFactory.CreateNormal())
            using (requester)
            {
                responder.RespondAsync<FirstRequest, FirstResponse>((request, context) =>
                {
                    return Task
                        .Run(() => Task.Delay(250))
                        .ContinueWith(t => new FirstResponse());
                }, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

                /* Test */
                /* Assert */
                await Assert.ThrowsAsync<TimeoutException>(() => requester.RequestAsync<FirstRequest, FirstResponse>());
            }
        }

        [Fact]
        public async Task Should_Not_Throw_If_Response_Is_Handled_Within_Time_Limit()
        {
            /* Setup */
            var requester = TestClientFactory.CreateNormal(ioc =>
            {
                ioc.AddSingleton(p =>
                {
                    var cfg = RawRabbitConfiguration.Local;
                    cfg.RequestTimeout = TimeSpan.FromMilliseconds(200);
                    return cfg;
                });
            });
            using (var responder = TestClientFactory.CreateNormal())
            using (requester)
            {
                responder.RespondAsync<FirstRequest, FirstResponse>((request, context) =>
                {
                    return Task.FromResult(new FirstResponse());
                }, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

                /* Test */
                await requester.RequestAsync<FirstRequest, FirstResponse>();

                /* Assert */
                Assert.True(true, "Response recieved without throwing.");
            }
        }
    }
}
