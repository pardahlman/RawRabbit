using System;
using System.Threading.Tasks;
using RawRabbit.Core.Configuration.Publish;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Message;

namespace RawRabbit.Core.Client
{
	public interface IBusClient
	{
		Task SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
			where T : MessageBase;

		Task PublishAsync<T>(T message, Action<IPublishConfigurationBuilder> configuration = null)
			where T : MessageBase;
	}
}
