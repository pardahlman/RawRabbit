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
using RawRabbit.Context.Enhancer;
using RawRabbit.Logging;

namespace RawRabbit.Operations
{
	public class Responder<TMessageContext> : OperatorBase, IResponder<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IContextEnhancer _contextEnhancer;
		private readonly ILogger _logger = LogManager.GetLogger<Responder<TMessageContext>>();
		private IModel _responseChannel;

		public Responder(IChannelFactory channelFactory, IConsumerFactory consumerFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider, IContextEnhancer contextEnhancer)
			: base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
			_contextProvider = contextProvider;
			_contextEnhancer = contextEnhancer;
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
				var body = Serializer.Deserialize<TRequest>(args.Body);
				var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[_contextProvider.ContextHeaderName]);
				_contextEnhancer.WireUpContextFeatures(context, consumer, args);

				return onMessage(body, context)
					.ContinueWith(payloadTask =>
					{
						if (!consumer.NackedDeliveryTags.Contains(args.DeliveryTag))
						{
							SendResponse(payloadTask.Result, args);
						}
					});
			};
			consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
		}

		private void SendResponse<TResponse>(TResponse request, BasicDeliverEventArgs requestPayload)
		{
			_responseChannel = (_responseChannel?.IsOpen ?? false)
				? _responseChannel
				: ChannelFactory.CreateChannel();
			var props = CreateResponseProps(requestPayload);
			_logger.LogDebug($"Sending reponse to request with correlation '{props.CorrelationId}' with message '{props.MessageId}'.");
			_responseChannel.BasicPublish(
				exchange: "",
				routingKey: requestPayload.BasicProperties.ReplyTo,
				basicProperties: props,
				body: Serializer.Serialize(request)
			);
		}

		private static IBasicProperties CreateResponseProps(BasicDeliverEventArgs requestPayload)
		{
			return new BasicProperties
			{
				MessageId = Guid.NewGuid().ToString(),
				CorrelationId = requestPayload.BasicProperties.CorrelationId
			};
		}

		public override void Dispose()
		{
			_logger.LogDebug("Disposing Responder.");
			base.Dispose();
			(_consumerFactory as IDisposable)?.Dispose();
			_responseChannel?.Dispose();
		}
	}
}
