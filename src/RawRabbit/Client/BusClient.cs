using System;
using System.Threading.Tasks;
using RawRabbit.Core.Client;
using RawRabbit.Core.Configuration;
using RawRabbit.Core.Message;

namespace RawRabbit.Client
{
	public class BusClient : IBusClient
	{
		public BusClient(): this(RawRabbitConfiguration.Default)
		{ /* No code here */}

		public BusClient(RawRabbitConfiguration configuration)
		{

		}

		public IDisposable SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, Action<ISubscriptionConfiguration> configuration = null) where T : MessageBase
		{
			throw new NotImplementedException();
		}

		public Task PublishAsync<T>(T message) where T : MessageBase
		{
			throw new NotImplementedException();
		}
	}
}
