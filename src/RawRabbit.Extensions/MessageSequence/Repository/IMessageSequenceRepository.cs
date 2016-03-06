using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.MessageSequence.Repository
{
	public interface IMessageSequenceRepository
	{
		Task<SequenceDefinition> GetAsync(Guid globalMessageId);
		Task<SequenceDefinition> GetOrCreateAsync(Guid globalMessageId);
		Task RemoveAsync(Guid globalMessageId);
		Task UpdateAsync(SequenceDefinition definition);
	}
}