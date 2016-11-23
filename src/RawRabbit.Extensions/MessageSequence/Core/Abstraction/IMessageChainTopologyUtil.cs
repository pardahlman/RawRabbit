using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.MessageSequence.Core.Abstraction
{
    public interface IMessageChainTopologyUtil
    {
        Task BindToExchange<T>(Guid globalMessaegId);
        Task BindToExchange(Type messageType, Guid globalMessaegId);
        Task UnbindFromExchange<T>(Guid globalMessaegId);
        Task UnbindFromExchange(Type messageType, Guid globalMessaegId);
    }
}
