using System.Threading.Tasks;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Publish;

namespace RawRabbit.Common
{
	public interface IRawPublisher
	{
		Task PublishAsync<T>(T message, PublishConfiguration config);
	}

	public class RawPublisher : RawOperatorBase, IRawPublisher
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IMessageSerializer _serializer;

		public RawPublisher(IChannelFactory channelFactory, IMessageSerializer serializer)
			: base(channelFactory)
		{
			_channelFactory = channelFactory;
			_serializer = serializer;
		}

		public Task PublishAsync<T>(T message, PublishConfiguration config)
		{
			var queueTask = DeclareQueueAsync(config.Queue);
			var exchangeTask = DeclareExchangeAsync(config.Exchange);
			var messageTask = CreateMessageAsync(message);

			return Task
				.WhenAll(queueTask, exchangeTask, messageTask)
				.ContinueWith(t => PublishAsync(messageTask.Result, config));
		}

		private Task PublishAsync(byte[] body, PublishConfiguration config)
		{
			return Task.Factory.StartNew(() =>
			{
				var channel = _channelFactory.GetChannel();
				channel.BasicPublish(
					exchange: config.Exchange.ExchangeName,
					routingKey: config.RoutingKey,
					basicProperties: channel.CreateBasicProperties(), //TODO: move this to config
					body: body
				);
			});
		}

		private Task<byte[]> CreateMessageAsync<T>(T message)
		{
			if (message == null)
			{
				return Task.FromResult(new byte[0]);
			}
			return Task.Factory.StartNew(() =>
				_serializer.Serialize(message)
			);
		}
	}
}
