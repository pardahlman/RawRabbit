using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.MessageSequence.Core.Abstraction
{
	public interface IMessageChainTopologyUtil
	{
		Task BindToExchange<T>(Guid globalMessageId);
		Task BindToExchange(Type messageType, Guid globalMessageId);
		Task UnbindFromExchange<T>(Guid globalMessageId);
		Task UnbindFromExchange(Type messageType, Guid globalMessageId);
	}
}
