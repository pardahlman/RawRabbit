using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
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
				.ContinueWith(t => BindQueue(config.ReplyQueue, config.Exchange, config.RoutingKey))
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
					var consumer = new QueueingBasicConsumer(channel);
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
					while (true)
					{
						var args = consumer.Queue.Dequeue();
						if (args.BasicProperties.CorrelationId != propsTask.Result.CorrelationId)
						{
								/*
								"You may ask, why should we ignore unknown messages in the callback queue,
								rather than failing with an error? It's due to a possibility of a race condition
								on the server side. Although unlikely, it is possible that the RPC server will
								die just after sending us the answer, but before sending an acknowledgment
								message for the request." 
								- https://www.rabbitmq.com/tutorials/tutorial-six-dotnet.html
								*/
							continue;
						}
						return Task
								.Run(() => Serializer.Deserialize<TResponse>(args.Body))
								.ContinueWith(t =>
								{
									channel.BasicCancel(consumer.ConsumerTag);
									responseTcs.SetResult(t.Result);
								});
					}
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
