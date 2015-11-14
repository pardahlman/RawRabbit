using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Operations.Contracts;
using RawRabbit.Serialization;
using RawRabbit.Consumer.Contract;

namespace RawRabbit.Operations
{
	public class Responder<TMessageContext> : OperatorBase, IResponder<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public Responder(IChannelFactory channelFactory, IConsumerFactory consumerFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider)
			: base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
			_contextProvider = contextProvider;
		}

		public void RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration cfg)
		{
			DeclareQueue(cfg.Queue);
			DeclareExchange(cfg.Exchange);
			BindQueue(cfg.Queue, cfg.Exchange, cfg.RoutingKey);
			ConfigureRespond(onMessage, cfg);
		}

		private void ConfigureRespond<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, IConsumerConfiguration cfg)
		{
			var consumer = _consumerFactory.CreateConsumer(cfg);
			consumer.OnMessageAsync = (o, args) =>
			{
				var bodyTask = Task.Run(() => Serializer.Deserialize<TRequest>(args.Body));
				var contextTask = _contextProvider
					.ExtractContextAsync(args.BasicProperties.Headers[_contextProvider.ContextHeaderName])
					.ContinueWith(ctxTask =>
					{
						var advancedCtx = ctxTask.Result as IAdvancedMessageContext;
						if (advancedCtx == null)
						{
							return ctxTask.Result;
						}
						advancedCtx.Nack = () =>
						{
							consumer.NackedDeliveryTags.Add(args.DeliveryTag);
							consumer.Model.BasicNack(args.DeliveryTag, false, true);
						};
						return ctxTask.Result;
					});

				return Task
					.WhenAll(bodyTask, contextTask)
					.ContinueWith(task =>	onMessage(bodyTask.Result, contextTask.Result)).Unwrap()
					.ContinueWith(payloadTask => SendResponseAsync(payloadTask.Result, args));
			};
			consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
		}

		private Task SendResponseAsync<TResponse>(TResponse result, BasicDeliverEventArgs requestPayload)
		{
			var propsTask = CreateReplyPropsAsync(requestPayload);
			var serializeTask = Task.Run(() => Serializer.Serialize(result));

			return Task
				.WhenAll(propsTask, serializeTask)
				.ContinueWith(task =>
				{
					var channel = ChannelFactory.CreateChannel();
					channel.BasicPublish(
						exchange: "",
						routingKey: requestPayload.BasicProperties.ReplyTo,
						basicProperties: propsTask.Result,
						body: serializeTask.Result
					);
					channel.Dispose();
				});
		}

		private static Task<IBasicProperties> CreateReplyPropsAsync(BasicDeliverEventArgs requestPayload)
		{
			return Task.Run(() =>
			{
				IBasicProperties replyProps = new BasicProperties
				{
					CorrelationId = requestPayload.BasicProperties.CorrelationId
				};
				return replyProps;
			});
		}

		public override void Dispose()
		{
			base.Dispose();
			(_consumerFactory as IDisposable)?.Dispose();
		}
	}
}
