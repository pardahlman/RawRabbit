using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Publish;
using RawRabbit.Core.Context;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Operations
{
	public interface IPublisher
	{
		Task PublishAsync<TMessage>(TMessage message, PublishConfiguration config);
	}

	public class Publisher<TMessageContext> : OperatorBase, IPublisher where TMessageContext : MessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public Publisher(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
		}

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
			return Task
				.Run(() => _contextProvider.GetMessageContextAsync())
				.ContinueWith(ctxTask =>
				{
					var channel = ChannelFactory.GetChannel();
					var properties = channel.CreateBasicProperties();
					properties.Headers = new Dictionary<string, object> {["message_context"] = ctxTask.Result};
					channel.BasicPublish(
						exchange: config.Exchange.ExchangeName,
						routingKey: config.RoutingKey,
						basicProperties: properties,
						body: body
					);
				});
		}
	}
}
