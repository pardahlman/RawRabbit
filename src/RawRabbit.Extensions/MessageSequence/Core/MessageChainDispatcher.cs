using System;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Context;
using RawRabbit.Extensions.MessageSequence.Core.Abstraction;
using RawRabbit.Extensions.MessageSequence.Model;
using RawRabbit.Extensions.MessageSequence.Repository;
using RawRabbit.Logging;

namespace RawRabbit.Extensions.MessageSequence.Core
{
	public class MessageChainDispatcher : IMessageChainDispatcher
	{
		private readonly IMessageSequenceRepository _sequenceRepository;
		private readonly ILogger _logger = LogManager.GetLogger<MessageChainDispatcher>();

		public MessageChainDispatcher(IMessageSequenceRepository sequenceRepository)
		{
			_sequenceRepository = sequenceRepository;
		}

		public Task AddMessageHandlerAsync<TMessage, TMessageContext>(Guid globalMessageId, Func<TMessage, TMessageContext, Task> func, StepOption configuration) where TMessageContext : IMessageContext
		{
			return _sequenceRepository
				.GetOrCreateAsync(globalMessageId)
				.ContinueWith(tSequence =>
				{
					tSequence.Result.StepDefinitions.Add(new StepDefinition
					{
						Handler = (o, context) => func((TMessage)o, (TMessageContext)context),
						Type = typeof(TMessage),
						Optional = configuration?.Optional ?? false,
						AbortsExecution = configuration?.AbortsExecution ?? false
					});
					return _sequenceRepository.UpdateAsync(tSequence.Result);
				})
				.Unwrap();
		}

		public async Task InvokeMessageHandlerAsync(Guid globalMessageId, object body, IMessageContext context)
		{
			var sequence = await _sequenceRepository.GetAsync(globalMessageId);
			if (sequence?.State.Aborted ?? true)
			{
				_logger.LogInformation($"Sequence for '{globalMessageId}' is either not found or aborted.");
				return;
			}

			var bodyType = body.GetType();
			var processedCount = sequence.State.Skipped.Count + sequence.State.Completed.Count;
			var unprocessedHandlers = sequence.StepDefinitions
				.Skip(processedCount)
				.ToList();
			var potentialOptional = unprocessedHandlers
				.TakeWhile(h => h.Optional);
			var firstMandatory = unprocessedHandlers
				.SkipWhile(h => h.Optional)
				.Take(1);
			var optionalFirst = unprocessedHandlers.FirstOrDefault()?.Optional ?? false;

			var potential = optionalFirst
				? potentialOptional.Concat(firstMandatory)
				: firstMandatory.Concat(potentialOptional);

			var match = potential.FirstOrDefault(p => p.Type == bodyType);
			if (match != null)
			{
				var skipped = potential
					.TakeWhile(p => p != match)
					.Select(s => new ExecutionResult
						{
							Time = DateTime.Now,
							Type = s.Type
						})
					.ToList();
				sequence.State.Skipped.AddRange(skipped);
				_logger.LogDebug($"Skipping {skipped.Count} message handlers for {globalMessageId}");
			}
			try
			{
				if (match == null)
				{
					_logger.LogInformation($"Unable to find applicable message handler of type '{bodyType.Name}' for '{globalMessageId}'");
					return;
				}
				_logger.LogInformation($"Invoking message handler of type '{bodyType.Name}' for '{globalMessageId}'");
				await match.Handler(body, context).ContinueWith(t =>
				{
					if (t.IsCompleted)
					{
						sequence.State.Completed.Add(new ExecutionResult
						{
							Type = bodyType,
							Time = DateTime.Now
						});
					}
				});
			}
			catch (Exception e)
			{
				_logger.LogError($"Message handler of type '{bodyType.Name}' for '{globalMessageId}' threw an unhandles exception.", e);
				sequence.State.Aborted = true;
				sequence.TaskCompletionSource.TrySetException(e);
			}
			if (match.AbortsExecution)
			{
				_logger.LogInformation($"Message handler of type '{bodyType.Name}' for '{globalMessageId}' aborts execution.");
				sequence.State.Aborted = true;
				sequence.TaskCompletionSource.TrySetResult(null);
			}
		}
	}
}