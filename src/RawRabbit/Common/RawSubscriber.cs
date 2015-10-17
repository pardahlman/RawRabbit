using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using RawRabbit.Core.Configuration.Subscribe;
using RawRabbit.Core.Message;

namespace RawRabbit.Common
{
	public interface IRawSubscriber
	{
		Task SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, SubscriptionConfiguration config) where T : MessageBase;
	}

	public class RawSubscriber : IRawSubscriber
	{
		private readonly IChannelFactory _channelFactory;

		public RawSubscriber(IChannelFactory channelFactory, IConfigurationEvaluator configEval)
		{
			_channelFactory = channelFactory;
		}

		public Task SubscribeAsync<T>(Func<T, MessageInformation, Task> subscribeMethod, SubscriptionConfiguration config) where T : MessageBase
		{
			var channel = _channelFactory.GetChannel();
			
			channel.ExchangeDeclare(
				exchange: config.ExchangeConfiguration.ExchangeName,
				type: config.ExchangeConfiguration.ExchangeType
			);

			channel.QueueBind(
				queue: config.QueueConfiguration.QueueName,
				exchange: config.ExchangeConfiguration.ExchangeName,
				routingKey: config.QueueConfiguration.RoutingKey
			);

			var consumer = new EventingBasicConsumer(channel);
			consumer.Received += (model, ea) =>
			{
				var body = ea.Body;
				var msgString = Encoding.UTF8.GetString(body);
				var message = JsonConvert.DeserializeObject<T>(msgString);
				subscribeMethod(message, null);
			};

			return Task.FromResult(true);
		}
	}
}
