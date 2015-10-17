using System;
using System.Threading.Tasks;
using RawRabbit.Core.Configuration;
using RawRabbit.Core.Message;

namespace RawRabbit.Core.Client
{
	public interface IBusClient
	{
		IDisposable SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, Action<ISubscriptionConfiguration> configuration = null)
			where T : MessageBase;

		Task PublishAsync<T>(T message)
			where T : MessageBase;
	}
}
