using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
	public class Responder<TMessageContext> : IResponder<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ITopologyProvider _topologyProvider;
		private readonly IConsumerFactory _consumerFactory;
		private readonly IMessageSerializer _serializer;
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;
		private readonly IContextEnhancer _contextEnhancer;
		private readonly IBasicPropertiesProvider _propertyProvider;
		private readonly List<IRawConsumer> _consumers;
		private readonly ILogger _logger = LogManager.GetLogger<Responder<TMessageContext>>();

		public Responder(
			IChannelFactory channelFactory,
			ITopologyProvider topologyProvider,
			IConsumerFactory consumerFactory,
			IMessageSerializer serializer,
			IMessageContextProvider<TMessageContext> contextProvider,
			IContextEnhancer contextEnhancer,
			IBasicPropertiesProvider propertyProvider)
		{
			_channelFactory = channelFactory;
			_topologyProvider = topologyProvider;
			_consumerFactory = consumerFactory;
			_serializer = serializer;
			_contextProvider = contextProvider;
			_contextEnhancer = contextEnhancer;
			_propertyProvider = propertyProvider;
			_consumers = new List<IRawConsumer>();
		}

		public void RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, ResponderConfiguration cfg)
		{
			var topologyTask = _topologyProvider.BindQueueAsync(cfg.Queue, cfg.Exchange, cfg.RoutingKey);
			var channelTask = _channelFactory.CreateChannelAsync();

			var respondTask = Task.WhenAll(topologyTask, channelTask)
				.ContinueWith(t =>
				{
					var consumer = _consumerFactory.CreateConsumer(cfg, channelTask.Result);
					_consumers.Add(consumer);
					consumer.OnMessageAsync = (o, args) =>
					{
						var body = _serializer.Deserialize<TRequest>(args.Body);
						var context = _contextProvider.ExtractContext(args.BasicProperties.Headers[PropertyHeaders.Context]);
						_contextEnhancer.WireUpContextFeatures(context, consumer, args);

						return onMessage(body, context)
							.ContinueWith(tResponse =>
							{
								if (tResponse.IsFaulted)
								{
									throw tResponse.Exception ?? new Exception();
								}
								if (consumer.NackedDeliveryTags.Contains(args.DeliveryTag))
								{
									return;
								}
								if (tResponse.Result == null)
								{
									return;
								}
								_logger.LogDebug($"Sending response to request with correlation '{args.BasicProperties.CorrelationId}'.");
								consumer.Model.BasicPublish(
									exchange: string.Empty,
									routingKey: args.BasicProperties.ReplyTo,
									basicProperties: _propertyProvider.GetProperties<TResponse>(p => p.CorrelationId = args.BasicProperties.CorrelationId),
									body: _serializer.Serialize(tResponse.Result)
								);
							});
					};
					consumer.Model.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, consumer);
				});

			Task.WaitAll(respondTask);
		}

		public void Dispose()
		{
			_logger.LogDebug("Disposing Responder.");
			(_consumerFactory as IDisposable)?.Dispose();
		}
	}
}
