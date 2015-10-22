using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common.Serialization;
using RawRabbit.Core.Configuration.Request;
using RawRabbit.Core.Message;

namespace RawRabbit.Common.Operations
{
	public interface IRequester
	{
		Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, RequestConfiguration config)
			where TRequest : MessageBase
			where TResponse : MessageBase;
	}

	public class Requester : OperatorBase, IRequester
	{
		public Requester(IChannelFactory channelFactory, IMessageSerializer serializer) : base(channelFactory, serializer)
		{ }

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message, RequestConfiguration config)
			where TRequest : MessageBase
			where TResponse : MessageBase
		{
			var replyQueueTask = DeclareQueueAsync(config.ReplyQueue);
			var exchangeTask = DeclareExchangeAsync(config.Exchange);

			return Task
				.WhenAll(replyQueueTask, exchangeTask)
				.ContinueWith(t => BindQueue(config.ReplyQueue, config.Exchange, config.RoutingKey))
				.ContinueWith(t => SendRequestAsync<TRequest, TResponse>(message, config))
				.Unwrap();
		}

		private Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest message, RequestConfiguration config)
		{
			var responseTcs = new TaskCompletionSource<TResponse>();
			var propsTask = GetRequestPropsAsync(config.ReplyQueue.QueueName);
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

		private Task<IBasicProperties> GetRequestPropsAsync(string queueName)
		{
			return Task.Run(() =>
			{
				var channel = ChannelFactory.GetChannel();
				var props = channel.CreateBasicProperties();
				props.ReplyTo = queueName;
				props.CorrelationId = Guid.NewGuid().ToString();
				return props;
			});
		}
	}
}
