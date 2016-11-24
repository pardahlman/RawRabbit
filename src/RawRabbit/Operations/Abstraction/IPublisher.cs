using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Legacy.Publish;

namespace RawRabbit.Operations.Abstraction
{
	public interface IPublisher
	{
		Task PublishAsync<TMessage>(TMessage message, Guid globalMessageId, PublishConfiguration config);
	}
}