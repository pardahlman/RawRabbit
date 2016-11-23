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

        public void AddMessageHandler<TMessage, TMessageContext>(Guid globalMessageId, Func<TMessage, TMessageContext, Task> func, StepOption configuration) where TMessageContext : IMessageContext
        {
            var sequence = _sequenceRepository.GetOrCreate(globalMessageId);
            sequence.StepDefinitions.Add(new StepDefinition
            {
                Handler = (o, context) => func((TMessage) o, (TMessageContext) context),
                Type = typeof(TMessage),
                Optional = configuration?.Optional ?? false,
                AbortsExecution = configuration?.AbortsExecution ?? false
            });
            _sequenceRepository.Update(sequence);
        }

        public void InvokeMessageHandler(Guid globalMessageId, object body, IMessageContext context)
        {
            var sequence = _sequenceRepository.Get(globalMessageId);
            if (sequence?.State.Aborted ?? true)
            {
                _logger.LogInformation($"Sequence for '{globalMessageId}' is either not found or aborted.");
                return;
            }
            var bodyType = body.GetType();
            var invokedIds = Enumerable
                .Concat(sequence.State.Completed, sequence.State.Skipped)
                .Select(s => s.StepId);
            var last = sequence.StepDefinitions.LastOrDefault();
            var notInvoked = sequence.StepDefinitions
                .Where(s => !invokedIds.Contains(s.Id))
                .Except(new [] {last})
                .ToList();
            if (notInvoked.All(s => s.Optional))
            {
                notInvoked.Add(last);
            }
            
            var match = notInvoked.FirstOrDefault(p => p.Type == bodyType);
            if (match == last)
            {
                var skipped = notInvoked
                    .TakeWhile(p => p != match)
                    .Select(s => new ExecutionResult
                        {
                            Time = DateTime.Now,
                            Type = s.Type,
                            StepId = s.Id
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
                sequence.State.Completed.Add(new ExecutionResult
                {
                    Type = bodyType,
                    Time = DateTime.Now,
                    StepId = match.Id
                });
                sequence.State.HandlerTasks.Add(match.Handler(body, context));
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