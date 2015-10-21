using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Respond;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Operations
{
	public interface IResponder
	{
		Task RespondAsync<TRequest, TResponse>(Func<TRequest, MessageInformation, Task<TResponse>> onMessage, ResponderConfiguration configuration)
			where TRequest : MessageBase
			where TResponse : MessageBase;
	}

	public class Responder : OperatorBase, IResponder
	{
		private readonly IChannelFactory _channelFactory;
		private readonly IMessageSerializer _serializer;

		public Responder(IChannelFactory channelFactory, IMessageSerializer serializer)
			: base(channelFactory)
		{
			_channelFactory = channelFactory;
			_serializer = serializer;
		}

		public Task RespondAsync<TRequest, TResponse>(Func<TRequest, MessageInformation, Task<TResponse>> onMessage, ResponderConfiguration configuration)
			where TRequest : MessageBase
			where TResponse : MessageBase
		{
			var queueTask = DeclareQueueAsync(configuration.Queue);
			var exchangeTask = DeclareExchangeAsync(configuration.Exchange);

			return Task
				.WhenAll(queueTask, exchangeTask)
				.ContinueWith(t => BindQueue(configuration))
				.ContinueWith(t => ConfigureRespond(onMessage, configuration));
		}

		private void BindQueue(ResponderConfiguration config)
		{
			if (config.Exchange.IsDefaultExchange())
			{
				return;
			}
			ChannelFactory
				.GetChannel()
				.QueueBind(
					queue: config.Queue.QueueName,
					exchange: config.Exchange.ExchangeName,
					routingKey: config.RoutingKey
				);
		}

		private void ConfigureRespond<TRequest, TResponse>(Func<TRequest, MessageInformation, Task<TResponse>> onMessage, ResponderConfiguration cfg)
		{
			var channel = ChannelFactory.GetChannel();
			ConfigureQosAsync(channel, cfg.PrefetchCount);
			var consumer = new EventingBasicConsumer(channel);
			channel.BasicConsume(cfg.Queue.QueueName, false, consumer);

			consumer.Received += (sender, args) =>
			{
				Task.Factory
					.StartNew(() => _serializer.Deserialize<TRequest>(args.Body))
					.ContinueWith(t => onMessage(t.Result, null)).Unwrap()
					.ContinueWith(payloadTask => SendRespondAsync(payloadTask.Result, args))
					.ContinueWith(t => BasicAck(channel, args.DeliveryTag));
			};
		}

		private Task SendRespondAsync<TResponse>(TResponse result, BasicDeliverEventArgs requestPayload)
		{
			var propsTask = CreateReplyPropsAsync(requestPayload);
			var serializeTask = Task.Factory.StartNew(() => _serializer.Serialize(result));

			return Task
				.WhenAll(propsTask, serializeTask)
				.ContinueWith(task =>
				{
					var channel = _channelFactory.GetChannel();
					channel.BasicPublish(
						exchange: requestPayload.Exchange,
						routingKey: requestPayload.BasicProperties.ReplyTo,
						basicProperties: propsTask.Result,
						body: serializeTask.Result
					);
				});
		}

		private Task<IBasicProperties> CreateReplyPropsAsync(BasicDeliverEventArgs requestPayload)
		{
			return Task.Factory.StartNew(() =>
			{
				var channel = _channelFactory.GetChannel();
				var replyProps = channel.CreateBasicProperties();
				replyProps.CorrelationId = requestPayload.BasicProperties.CorrelationId;
				return replyProps;
			});
		}
	}
}
