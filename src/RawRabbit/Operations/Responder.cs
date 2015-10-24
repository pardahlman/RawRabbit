using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Serialization;

namespace RawRabbit.Operations
{
	public interface IResponder<out TMessageContext> where TMessageContext : IMessageContext
	{
		Task RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration configuration);
	}

	public class Responder<TMessageContext> : OperatorBase, IResponder<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly ConcurrentBag<string> _processedButNotAcked;

		public Responder(IChannelFactory channelFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_contextProvider = contextProvider;
			_processedButNotAcked = new ConcurrentBag<string>();
		}

		public Task RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration configuration)
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
			channel.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);

			consumer.Received += (sender, args) =>
			{
				if (_processedButNotAcked.Contains(args.BasicProperties.MessageId))
				{
					if (TryBasicAck(channel, args))
						return;
				}
				var bodyTask = Task.Run(() => Serializer.Deserialize<TRequest>(args.Body));
				var contextTask = _contextProvider.ExtractContextAsync(args.BasicProperties.Headers[_contextProvider.ContextHeaderName]);
				Task
					.WhenAll(bodyTask, contextTask)
					.ContinueWith(task => onMessage(bodyTask.Result, contextTask.Result)).Unwrap()
					.ContinueWith(payloadTask => SendRespondAsync(payloadTask.Result, args))
					.ContinueWith(t =>
					{
						if (cfg.NoAck)
							return; // TODO: make sure NoAck is honoured when binding queue
						if (!TryBasicAck(channel, args))
						{
							RespondAsync(onMessage, cfg);
						}
					});
			};
		}

		private bool TryBasicAck(IModel channel, BasicDeliverEventArgs args)
		{
			try
			{
				BasicAck(channel, args.DeliveryTag);
				return true;
			}
			catch (AlreadyClosedException)
			{
				_processedButNotAcked.Add(args.BasicProperties.MessageId);
				return false;
			}
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
