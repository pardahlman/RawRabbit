using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common.Serialization;
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
			var basicQosTask = ConfigureQosAsync(configuration);

			return Task
				.WhenAll(queueTask, exchangeTask, basicQosTask)
				.ContinueWith(t => ConfigureRespondAsync(onMessage, configuration));
		}

		private Task ConfigureRespondAsync<TRequest, TResponse>(Func<TRequest, MessageInformation, Task<TResponse>> onMessage, ResponderConfiguration cfg)
		{
			return Task.Factory.StartNew(() =>
			{
				var channel = _channelFactory.GetChannel();
				var consumer = new EventingBasicConsumer(channel);
				channel.BasicConsume(cfg.Queue.QueueName, false, consumer);
				
				consumer.Received += (sender, args) =>
				{
					Task.Factory
						.StartNew(() => _serializer.Deserialize<TRequest>(args.Body))
						.ContinueWith(t => onMessage(t.Result, null)
							.ContinueWith(responseTask =>
							{
								var ackTask = BasicAckAsync(args.DeliveryTag);
								var respondTask = SendRespondAsync(responseTask.Result, args);
								return Task.WhenAll(ackTask, respondTask);
							})
						);
				};
			});
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
						exchange:requestPayload.Exchange,
						routingKey: propsTask.Result.ReplyTo,
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

		private Task ConfigureQosAsync(ResponderConfiguration config)
		{
			return Task.Factory.StartNew(() =>
			{
				_channelFactory
					.GetChannel()
					.BasicQos(
						prefetchSize: 0, //TODO : what is this?
						prefetchCount: config.PrefetchCount,
						global: false // https://www.rabbitmq.com/consumer-prefetch.html
				);
			});
		}

	}
}
