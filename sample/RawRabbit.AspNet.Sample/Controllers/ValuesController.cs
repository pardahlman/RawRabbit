using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RawRabbit.Context;
using RawRabbit.Messages.Sample;
using RawRabbit.Operations.MessageSequence;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace RawRabbit.AspNet.Sample.Controllers
{
	public class ValuesController : Controller
	{
		private readonly IBusClient _busClient;
		private readonly Random _random;
		private readonly ILogger<ValuesController> _logger;

		public ValuesController(IBusClient legacyBusClient, ILoggerFactory loggerFactory)
		{
			_busClient = legacyBusClient;
			_logger = loggerFactory.CreateLogger<ValuesController>();
			_random = new Random();
		}

		[HttpGet]
		[Route("api/values")]
		public Task<List<string>> GetAsync()
		{
			_logger.LogDebug("Recieved Value Request.");
			var valueSequence = _busClient.ExecuteSequence<MessageContext, ValuesCalculated>(s => s
				.PublishAsync(new ValuesRequested
					{
						NumberOfValues = _random.Next(1,10)
					})
				.When<ValueCreationFailed>(
					(failed, context) =>
					{
						_logger.LogWarning("Unable to create Values. Exception: {0}", failed.Exception);
						return Task.FromResult(true);
					}, it => it.AbortsExecution())
				.Complete<ValuesCalculated>()
			);

			return valueSequence.Task.ContinueWith(tResponse =>
			{
				if (tResponse.IsFaulted)
					throw new Exception("No response recieved. Is the Console App started?");
				
				_logger.LogInformation("Successfully created {valueCount} values", tResponse.Result.Values.Count);
				return valueSequence.Aborted
					? new List<string>()
					: tResponse.Result.Values;
			});
		}

		[HttpGet("api/values/{id}")]
		public Task<string> GetAsync(int id)
		{
			_logger.LogInformation("Requesting Value with id {valueId}", id);
			return _busClient
				.RequestAsync<ValueRequest, ValueResponse>(new ValueRequest {Value = id})
				.ContinueWith(tResponse => tResponse.Result.Value);
		}
	}
}
