using System;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.MessageSequence.Core.Abstraction
{
	public interface IMessageChainTopologyUtil
	{
		Task BindToExchange<T>();
		void Unregister(Guid globalRequestId);
	}
}