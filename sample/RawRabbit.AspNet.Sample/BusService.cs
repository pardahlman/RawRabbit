using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using RawRabbit.Messages.Sample;
using RawRabbit.Common;

namespace RawRabbit.AspNet.Sample
{
	public class BusService : IHostedService
	{
		private readonly IBusClient _busClient;
		private readonly ILogger _logger;

		public BusService(IBusClient busClient, ILoggerFactory loggerFactory)
		{
			_busClient = busClient;

			_logger = loggerFactory.CreateLogger<BusService>();
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await _busClient.SubscribeAsync<TestMessage>((message) =>
			{
				_logger.LogInformation($"TestMessage Handled. Id='{message.Id}'.");

				return Task.CompletedTask;
			});

			await _busClient.RespondAsync<TestRequestMessage, TestResponseMessage>((message) =>
			{
				_logger.LogInformation($"TestRequestMessage Handled. Id='{message.Id}'.");

				return Task.FromResult(new TestResponseMessage
				{
					Id = message.Id
				});
			});
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
