using System.Threading.Tasks;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Publish;

namespace RawRabbit.Common.Operations
{
	public interface IPublisher
	{
		Task PublishAsync<T>(T message, PublishConfiguration config);
	}

	public class Publisher : OperatorBase , IPublisher
	{

		public Publisher(IChannelFactory channelFactory, IMessageSerializer serializer)
			: base(channelFactory, serializer)
		{ }

		public Task PublishAsync<T>(T message, PublishConfiguration config)
		{
			var queueTask = DeclareQueueAsync(config.Queue);
			var exchangeTask = DeclareExchangeAsync(config.Exchange);
			var messageTask = CreateMessageAsync(message);

			return Task
				.WhenAll(queueTask, exchangeTask, messageTask)
				.ContinueWith(t => PublishAsync(messageTask.Result, config))
				.Unwrap();
		}

		private Task PublishAsync(byte[] body, PublishConfiguration config)
		{
			return Task.Factory.StartNew(() =>
			{
				var channel = ChannelFactory.GetChannel();
				channel.BasicPublish(
					exchange: config.Exchange.ExchangeName,
					routingKey: config.RoutingKey,
					basicProperties: channel.CreateBasicProperties(), //TODO: move this to config
					body: body
				);
			});
		}
	}
}
