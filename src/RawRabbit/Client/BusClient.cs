using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Core.Client;
using RawRabbit.Core.Configuration.Publish;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Message;

namespace RawRabbit.Client
{
	public class BusClient : IBusClient
	{
		private readonly IConfigurationEvaluator _configEval;
		private readonly IRawSubscriber _subscriber;
		private readonly IRawPublisher _publisher;

		public BusClient(
			IConfigurationEvaluator configEval,
			IRawSubscriber subscriber,
			IRawPublisher publisher)
		{
			_configEval = configEval;
			_subscriber = subscriber;
			_publisher = publisher;
		}

		public IDisposable SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null) where T : MessageBase
		{
			var config = _configEval.GetConfiguration<T>(configuration);
			_subscriber.SubscribeAsync(subscribeMethod, config);
			return null;
		}

		public Task PublishAsync<T>(T message, Action<IPublishConfigurationBuilder> configuration = null) where T : MessageBase
		{
			var config = _configEval.GetConfiguration<T>(configuration);
			return _publisher.PublishAsync(message, config);
		}
	}
}
