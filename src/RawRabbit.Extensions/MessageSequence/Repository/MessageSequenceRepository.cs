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

		public Task<SequenceDefinition> GetAsync(Guid globalMessageId)
		{
			SequenceDefinition sequence;
			_sequenceDictionary.TryGetValue(globalMessageId, out sequence);
			return Task.FromResult(sequence);
		}

		public Task<SequenceDefinition> GetOrCreateAsync(Guid globalMessageId)
		{
			SequenceDefinition sequence;
			if (_sequenceDictionary.TryGetValue(globalMessageId, out sequence))
			{
				return Task.FromResult(sequence);
			}
			sequence = new SequenceDefinition() { GlobalMessageId = globalMessageId };
			return !_sequenceDictionary.TryAdd(globalMessageId, sequence)
				? GetAsync(globalMessageId)
				: Task.FromResult(sequence);
		}

		public Task RemoveAsync(Guid globalMessageId)
		{
			SequenceDefinition removed;
			_sequenceDictionary.TryRemove(globalMessageId, out removed);
			return Task.FromResult(true);
		}

		public Task UpdateAsync(SequenceDefinition definition)
		{
			/* Do noting in this in-memory implementation */
			return Task.FromResult(true);
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
