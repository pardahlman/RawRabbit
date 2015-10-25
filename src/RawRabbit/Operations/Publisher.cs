using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public class Publisher<TMessageContext> : OperatorBase, IPublisher where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public Publisher(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
		}

		public Task PublishAsync<T>(T message, Guid globalMessageId, PublishConfiguration config)
		{
			var queueTask = DeclareQueueAsync(config.Queue);
			var exchangeTask = DeclareExchangeAsync(config.Exchange);
			var messageTask = CreateMessageAsync(message);

			return Task
				.WhenAll(queueTask, exchangeTask, messageTask)
				.ContinueWith(t => PublishAsync(messageTask.Result, config, globalMessageId))
				.Unwrap();
		}

		private Task PublishAsync(byte[] body, PublishConfiguration config, Guid globalMessageId)
		{
			return Task
				.Run(() => _contextProvider.GetMessageContextAsync(globalMessageId))
				.ContinueWith(ctxTask =>
				{
					var channel = ChannelFactory.GetChannel();
					var properties = channel.CreateBasicProperties();
					properties.Headers = new Dictionary<string, object> {[_contextProvider.ContextHeaderName] = ctxTask.Result};
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
