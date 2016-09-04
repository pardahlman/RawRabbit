using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Common;
using RawRabbit.IntegrationTests.TestMessages;
using RawRabbit.Logging;
using RawRabbit.vNext;
using Xunit;

namespace RawRabbit.IntegrationTests.SimpleUse
{
	public class MultipleRequestsTests
	{
		[Fact]
		public async Task Should_Just_Work()
		{
			/* Setup */
			var voidLogger = new VoidLogger();
			using (var requester = BusClientFactory.CreateDefault(ioc => ioc.AddSingleton<ILoggerFactory>(p => new LoggerFactory(s => voidLogger))))
			using (var responder = BusClientFactory.CreateDefault(ioc => ioc.AddSingleton<ILoggerFactory>(p => new LoggerFactory(s => voidLogger))))
			{
				const int numberOfCalls = 10000;
				var array = new Task[numberOfCalls];
				responder.RespondAsync<FirstRequest, FirstResponse>((req, i) =>
					Task.FromResult(new FirstResponse { Infered = Guid.NewGuid() }),
					cfg => cfg.WithQueue(q => q.WithAutoDelete())
				);

				/* Test */
				var sw = new Stopwatch();
				sw.Start();
				for (var i = 0; i < numberOfCalls; i++)
				{
					var response = requester.RequestAsync<FirstRequest, FirstResponse>();
					array[i] = response;
				}
				Task.WaitAll(array);
				sw.Stop();
				var ids = array
					.OfType<Task<FirstResponse>>()
					.Select(b => b.Result.Infered)
					.Where(id => id != Guid.Empty)
					.Distinct()
					.ToList();

				/* Assert */
				Assert.Equal(expected: numberOfCalls, actual: ids.Count);
			}
		}

		[Fact]
		public async Task Should_Work_For_Multiple_Types()
		{
			/* Setup */
			var voidLogger = new VoidLogger();
			using (var requester = BusClientFactory.CreateDefault(ioc => ioc.AddSingleton<ILoggerFactory>(p => new LoggerFactory(s => voidLogger))))
			using (var responder = BusClientFactory.CreateDefault(ioc => ioc.AddSingleton<ILoggerFactory>(p => new LoggerFactory(s => voidLogger))))
			{
				const int numberOfCalls = 10000;
				var firstResponseTasks = new Task[numberOfCalls];
				var secondResponseTasks = new Task[numberOfCalls];
				responder.RespondAsync<FirstRequest, FirstResponse>((req, i) =>
					Task.FromResult(new FirstResponse { Infered = Guid.NewGuid() })
				, cfg => cfg.WithQueue(q => q.WithAutoDelete()));
				responder.RespondAsync<SecondRequest, SecondResponse>((request, context) =>
					Task.FromResult(new SecondResponse { Source = Guid.NewGuid() })
				, cfg => cfg.WithQueue(q => q.WithAutoDelete()));

				/* Test */
				for (var i = 0; i < numberOfCalls; i++)
				{
					var firstResponse = requester.RequestAsync<FirstRequest, FirstResponse>();
					var secondResponse = requester.RequestAsync<SecondRequest, SecondResponse>();
					firstResponseTasks[i] = firstResponse;
					secondResponseTasks[i] = secondResponse;
				}
				Task.WaitAll(firstResponseTasks.Concat(secondResponseTasks).ToArray());
				var firstIds = firstResponseTasks
					.OfType<Task<FirstResponse>>()
					.Select(b => b.Result.Infered)
					.Where(id => id != Guid.Empty)
					.Distinct()
					.ToList();
				var secondIds = secondResponseTasks
					.OfType<Task<SecondResponse>>()
					.Select(b => b.Result.Source)
					.Where(id => id != Guid.Empty)
					.Distinct()
					.ToList();
				/* Assert */
				Assert.Equal(expected: numberOfCalls, actual: firstIds.Count);
				Assert.Equal(expected: numberOfCalls, actual: secondIds.Count);
			}
		}
	}
}
