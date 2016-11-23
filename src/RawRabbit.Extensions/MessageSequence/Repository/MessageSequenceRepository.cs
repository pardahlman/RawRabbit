using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Extensions.MessageSequence.Model;

namespace RawRabbit.Extensions.MessageSequence.Repository
{
    public class MessageSequenceRepository : IMessageSequenceRepository
    {
        private readonly ConcurrentDictionary<Guid, SequenceDefinition> _sequenceDictionary;

        public MessageSequenceRepository()
        {
            _sequenceDictionary = new ConcurrentDictionary<Guid, SequenceDefinition>();
        }

        public SequenceDefinition Get(Guid globalMessageId)
        {
            SequenceDefinition sequence;
            _sequenceDictionary.TryGetValue(globalMessageId, out sequence);
            return sequence;
        }

        public SequenceDefinition GetOrCreate(Guid globalMessageId)
        {
            SequenceDefinition sequence;
            if (_sequenceDictionary.TryGetValue(globalMessageId, out sequence))
            {
                return sequence;
            }
            return _sequenceDictionary.GetOrAdd(globalMessageId, new SequenceDefinition {GlobalMessageId = globalMessageId});
        }

        public void Remove(Guid globalMessageId)
        {
            SequenceDefinition removed;
            _sequenceDictionary.TryRemove(globalMessageId, out removed);
        }

        public void Update(SequenceDefinition definition)
        {
            /* Do noting in this in-memory implementation */
        }
    }

    public class SequenceDefinition
    {
        public Guid GlobalMessageId { get; set; }
        public List<StepDefinition> StepDefinitions { get; set; }
        public ExecutionState State { get; set; }
        public TaskCompletionSource<object> TaskCompletionSource { get; set; }

        public SequenceDefinition()
        {
            StepDefinitions = new List<StepDefinition>();
            State = new ExecutionState();
            TaskCompletionSource = new TaskCompletionSource<object>();
        }
    }
}
