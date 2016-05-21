using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RawRabbit.Extensions.MessageSequence;
using RawRabbit.Messages.Sample;

namespace RawRabbit.AspNet.Sample.Controllers
{
	using IBusClient = Extensions.Client.IBusClient;

	[Route("api/[controller]")]
	public class ValuesController : Controller
	{
		private readonly IBusClient _busClient;
		private readonly Random _random;

		public ValuesController(IBusClient busClient)
		{
			_busClient = busClient;
			_random = new Random();
		}

		// GET api/values
		[HttpGet]
		public Task<List<string>> GetAsync()
		{
			var valueSequence = _busClient.ExecuteSequence(s => s
				.PublishAsync(new ValuesRequested {NumberOfValues = _random.Next(1,10)})
				.When<ValueCreationFailed>(
					(failed, context) => Task.FromResult(true),
					it => it.AbortsExecution())
				.Complete<ValuesCalculated>()
			);

			return valueSequence.Task.ContinueWith(tResponse =>
			{
				if (tResponse.IsFaulted)
				{
					throw new Exception("No response recieved. Is the Console App started?");
				}

				return valueSequence.Aborted
					? new List<string>()
					: tResponse.Result.Values;
			});
		}

		// GET api/values/5
		[HttpGet("{id}")]
		public Task<string> GetAsync(int id)
		{
			return _busClient
				.RequestAsync<ValueRequest, ValueResponse>(new ValueRequest {Value = id})
				.ContinueWith(tResponse => tResponse.Result.Value);
		}
	}
}
