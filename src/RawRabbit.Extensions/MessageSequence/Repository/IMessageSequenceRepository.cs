using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.MessageSequence.Repository
{
    public interface IMessageSequenceRepository
    {
        SequenceDefinition Get(Guid globalMessageId);
        SequenceDefinition GetOrCreate(Guid globalMessageId);
        void Remove(Guid globalMessageId);
        void Update(SequenceDefinition definition);
    }
}