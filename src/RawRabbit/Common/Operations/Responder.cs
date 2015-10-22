using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Respond;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Operations
{
	public interface IResponder<out TMessageContext> where TMessageContext : MessageContext
	{
		Task RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration configuration)
			where TRequest : MessageBase
			where TResponse : MessageBase;
	}

	public class Responder<TMessageContext> : OperatorBase, IResponder<TMessageContext> where TMessageContext: MessageContext
	{
		public Responder(IChannelFactory channelFactory, IMessageSerializer serializer)
			: base(channelFactory, serializer)
		{
		}

		public Task RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration configuration)
			where TRequest : MessageBase
			where TResponse : MessageBase
		{
			var queueTask = DeclareQueueAsync(configuration.Queue);
			var exchangeTask = DeclareExchangeAsync(configuration.Exchange);

			return Task
				.WhenAll(queueTask, exchangeTask)
				.ContinueWith(t => BindQueue(configuration.Queue, configuration.Exchange, configuration.RoutingKey))
				.ContinueWith(t => ConfigureRespond(onMessage, configuration));
		}

		private void ConfigureRespond<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration cfg)
		{
			var channel = ChannelFactory.GetChannel();
			ConfigureQosAsync(channel, cfg.PrefetchCount);
			var consumer = new EventingBasicConsumer(channel);
			channel.BasicConsume(cfg.Queue.QueueName, false, consumer);

			consumer.Received += (sender, args) =>
			{
				Task
					.Run(() => Serializer.Deserialize<TRequest>(args.Body))
					.ContinueWith(t => onMessage(t.Result, null)).Unwrap()
					.ContinueWith(payloadTask => SendRespondAsync(payloadTask.Result, args))
					.ContinueWith(t => BasicAck(channel, args.DeliveryTag));
			};
		}

		private Task SendRespondAsync<TResponse>(TResponse result, BasicDeliverEventArgs requestPayload)
		{
			var propsTask = CreateReplyPropsAsync(requestPayload);
			var serializeTask = Task.Run(() => Serializer.Serialize(result));
			
			return Task
				.WhenAll(propsTask, serializeTask)
				.ContinueWith(task =>
				{
					var channel = ChannelFactory.GetChannel();
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
			return Task.Run(() =>
			{
				var channel = ChannelFactory.GetChannel();
				var replyProps = channel.CreateBasicProperties();
				replyProps.CorrelationId = requestPayload.BasicProperties.CorrelationId;
				return replyProps;
			});
		}
	}
}
