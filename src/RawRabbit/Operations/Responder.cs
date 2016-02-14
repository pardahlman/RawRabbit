using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Context;
using RawRabbit.Context.Provider;
using RawRabbit.Serialization;
using RawRabbit.Context.Enhancer;
using RawRabbit.Logging;
using RawRabbit.Operations.Abstraction;

namespace RawRabbit.Operations
{
	public class Responder<TMessageContext> : OperatorBase, IResponder<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IContextEnhancer _contextEnhancer;
		private readonly IBasicPropertiesProvider _propertyProvider;
		private readonly ILogger _logger = LogManager.GetLogger<Responder<TMessageContext>>();
		private IModel _responseChannel;

		public Responder(IChannelFactory channelFactory, IConsumerFactory consumerFactory, IMessageSerializer serializer, IMessageContextProvider<TMessageContext> contextProvider, IContextEnhancer contextEnhancer, IBasicPropertiesProvider propertyProvider)
			: base(channelFactory, serializer)
		{
			_consumerFactory = consumerFactory;
			_contextProvider = contextProvider;
			_contextEnhancer = contextEnhancer;
			_propertyProvider = propertyProvider;
		}

		public void RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration cfg)
		{
			var channel = ChannelFactory.CreateChannel();
			DeclareQueue(cfg.Queue, channel);
			DeclareExchange(cfg.Exchange, channel);
			BindQueue(cfg.Queue, cfg.Exchange, cfg.RoutingKey, channel);
			var consumer = _consumerFactory.CreateConsumer(cfg, channel);
			consumer.OnMessageAsync = (o, args) =>
			{
				var body = Serializer.Deserialize<TRequest>(args.Body);
				var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[PropertyHeaders.Context]);
				_contextEnhancer.WireUpContextFeatures(context, consumer, args);

				return onMessage(body, context)
					.ContinueWith(responseTask =>
					{
						if (consumer.NackedDeliveryTags.Contains(args.DeliveryTag))
						{
							return;
						}
						if (responseTask.Result == null)
						{
							return;
						}
						SendResponse(responseTask.Result, args);
					});
			};
			consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
		}

		private void SendResponse<TResponse>(TResponse request, BasicDeliverEventArgs requestPayload)
		{
			_responseChannel = (_responseChannel?.IsOpen ?? false)
				? _responseChannel
				: ChannelFactory.CreateChannel();
			_logger.LogDebug($"Sending response to request with correlation '{requestPayload.BasicProperties.CorrelationId}'.");
			_responseChannel.BasicPublish(
				exchange: "",
				routingKey: requestPayload.BasicProperties.ReplyTo,
				basicProperties: _propertyProvider.GetProperties<TResponse>(p => p.CorrelationId = requestPayload.BasicProperties.CorrelationId),
				body: Serializer.Serialize(request)
			);
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
