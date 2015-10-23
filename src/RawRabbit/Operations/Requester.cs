using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Request;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public interface IRequester
	{
		Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration config);
	}

	public class Requester<TMessageContext> : OperatorBase, IRequester where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public Requester(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider) : base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration config)
		{
			var replyQueueTask = DeclareQueueAsync(config.ReplyQueue);
			var exchangeTask = DeclareExchangeAsync(config.Exchange);

			return Task
				.WhenAll(replyQueueTask, exchangeTask)
				.ContinueWith(t => BindQueue(config.ReplyQueue, config.Exchange, config.ReplyQueue.QueueName)) //TODO : Gosh. Make sure routing key is unique for an rpc.
				.ContinueWith(t => SendRequestAsync<TRequest, TResponse>(message, globalMessageId, config))
				.Unwrap();
		}

		private Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest message, Guid globalMessageId, RequestConfiguration config)
		{
			var responseTcs = new TaskCompletionSource<TResponse>();
			var propsTask = GetRequestPropsAsync(config.ReplyQueue.QueueName, globalMessageId);
			var bodyTask = CreateMessageAsync(message);

			Task
				.WhenAll(propsTask, bodyTask)
				.ContinueWith(task =>
				{
					var channel = ChannelFactory.GetChannel();
					var consumer = new EventingBasicConsumer(channel);
					consumer.Received += (sender, args) =>
					{
						if (args.BasicProperties.CorrelationId != propsTask.Result.CorrelationId)
						{
							return;
						}
						Task
							.Run(() => Serializer.Deserialize<TResponse>(args.Body))
							.ContinueWith(t =>
							{
								channel.BasicCancel(consumer.ConsumerTag);
								responseTcs.SetResult(t.Result);
							});
					};
					channel.BasicConsume(
							queue: config.ReplyQueue.QueueName,
							noAck: true,
							consumer: consumer
						);
					channel.BasicPublish(
							exchange: config.Exchange.ExchangeName,
							routingKey: config.RoutingKey,
							basicProperties: propsTask.Result,
							body: bodyTask.Result
						);
				}, TaskContinuationOptions.None);
			return responseTcs.Task;
		}

		private Task<IBasicProperties> GetRequestPropsAsync(string queueName, Guid globalMessageId)
		{
			return Task
				.Run(() => _contextProvider.GetMessageContextAsync(globalMessageId))
				.ContinueWith(ctxTask =>
				{
					var channel = ChannelFactory.GetChannel();
					var props = channel.CreateBasicProperties();
					props.ReplyTo = queueName;
					props.CorrelationId = Guid.NewGuid().ToString();
					props.Headers = new Dictionary<string, object>
					{
						{ _contextProvider.ContextHeaderName, ctxTask.Result}
					};
					return props;
				});
		}
	}
}
