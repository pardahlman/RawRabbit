using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Framing;
using RawRabbit.Common;
using RawRabbit.Compatibility.Legacy.Configuration;
using RawRabbit.Compatibility.Legacy.Configuration.Publish;
using RawRabbit.Compatibility.Legacy.Configuration.Request;
using RawRabbit.Compatibility.Legacy.Configuration.Respond;
using RawRabbit.Compatibility.Legacy.Configuration.Subscribe;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Configuration.Queue;
using RawRabbit.Enrichers.MessageContext.Context;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;
using RawRabbit.Subscription;

namespace RawRabbit.Compatibility.Legacy
{
	public class BusClient<TMessageContext> : IBusClient<TMessageContext>, IDisposable where TMessageContext : IMessageContext
	{
		private readonly RawRabbit.IBusClient _client;
		private readonly IConfigurationEvaluator _configEval;

		public BusClient(RawRabbit.IBusClient client, IConfigurationEvaluator configEval)
		{
			_client = client;
			_configEval = configEval;
		}

		public ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
		{
			var config = _configEval.GetConfiguration<T>(configuration);
			var exchangeCfg = config.Exchange.AssumeInitialized
				? null
				: new ExchangeDeclaration
				{
					AutoDelete = config.Exchange.AutoDelete,
					Durable = config.Exchange.Durable,
					ExchangeType = config.Exchange.ExchangeType,
					Name = config.Exchange.ExchangeName,
					Arguments = config.Exchange.Arguments
				};
			var queueCfg = config.Queue.AssumeInitialized
				? null
				: new QueueDeclaration
				{
					Arguments = config.Queue.Arguments,
					Durable = config.Queue.Durable,
					AutoDelete = config.Queue.AutoDelete,
					Exclusive = config.Queue.Exclusive,
					Name = config.Queue.FullQueueName
				};

			var consumerCfg = new ConsumerConfiguration
			{
				Exchange = exchangeCfg,
				Queue = queueCfg,
				Consume = new ConsumeConfiguration
				{
					ConsumerTag = Guid.NewGuid().ToString(),
					ExchangeName = config.Exchange.ExchangeName,
					QueueName = config.Queue.FullQueueName,
					AutoAck = config.AutoAck,
					RoutingKey = config.RoutingKey,
					PrefetchCount = config.PrefetchCount
				}
			};
			Action<IPipeContext> ctxAction = context =>
			{
				context.Properties.TryAdd(PipeKey.ConsumerConfiguration, consumerCfg);
				context.Properties.TryAdd(PipeKey.ConsumeConfiguration, consumerCfg.Consume);
				context.Properties.TryAdd(PipeKey.QueueDeclaration, consumerCfg.Queue);
				context.Properties.TryAdd(PipeKey.ExchangeDeclaration, consumerCfg.Exchange);
			};

			return _client
				.SubscribeAsync(subscribeMethod, ctxAction)
				.ContinueWith(tContext => tContext.Result.GetSubscription())
				.GetAwaiter()
				.GetResult();
		}

		public Task PublishAsync<T>(T message = default(T), Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null)
		{
			var config = _configEval.GetConfiguration<T>(configuration);
			var exchangeCfg = config.Exchange.AssumeInitialized
				? null
				: new ExchangeDeclaration
				{
					AutoDelete = config.Exchange.AutoDelete,
					Durable = config.Exchange.Durable,
					ExchangeType = config.Exchange.ExchangeType,
					Name = config.Exchange.ExchangeName,
					Arguments = config.Exchange.Arguments
				};

			var publisherCfg = new PublisherConfiguration
			{
				Exchange = exchangeCfg,
				ExchangeName = config.Exchange.ExchangeName,
				RoutingKey = config.RoutingKey,
				BasicProperties = new BasicProperties(),
				Mandatory = config.BasicReturn != null,
				MandatoryCallback = config.BasicReturn
			};
			config.PropertyModifier?.Invoke(publisherCfg.BasicProperties);

			Action<IPipeContext> ctxAction = context =>
			{
				context.Properties.TryAdd(PipeKey.PublisherConfiguration, publisherCfg);
				context.Properties.TryAdd(PipeKey.BasicPublishConfiguration, publisherCfg);
				context.Properties.TryAdd(PipeKey.ExchangeDeclaration, exchangeCfg);
				context.Properties.TryAdd(PipeKey.ReturnedMessageCallback, publisherCfg.MandatoryCallback);
			};
			return _client.PublishAsync(message: message, context: ctxAction);
		}

		public ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
		{
			var config = _configEval.GetConfiguration<TRequest, TResponse>(configuration);
			var exchangeCfg = config.Exchange.AssumeInitialized
				? null
				: new ExchangeDeclaration
				{
					AutoDelete = config.Exchange.AutoDelete,
					Durable = config.Exchange.Durable,
					ExchangeType = config.Exchange.ExchangeType,
					Name = config.Exchange.ExchangeName,
					Arguments = config.Exchange.Arguments
				};
			var queueCfg = config.Queue.AssumeInitialized
				? null
				: new QueueDeclaration
				{
					Arguments = config.Queue.Arguments,
					Durable = config.Queue.Durable,
					AutoDelete = config.Queue.AutoDelete,
					Exclusive = config.Queue.Exclusive,
					Name = config.Queue.FullQueueName
				};

			var respondCfg = new ConsumerConfiguration
			{
				Exchange = exchangeCfg,
				Queue = queueCfg,
				Consume = new ConsumeConfiguration
				{
					ConsumerTag = Guid.NewGuid().ToString(),
					ExchangeName = config.Exchange.ExchangeName,
					QueueName = config.Queue.FullQueueName,
					AutoAck = config.AutoAck,
					RoutingKey = config.RoutingKey,
					PrefetchCount = config.PrefetchCount
				}
			};
			Action<IPipeContext> ctxAction = context =>
			{
				context.Properties.Add(PipeKey.ConsumerConfiguration, respondCfg);
				context.Properties.Add(PipeKey.ConsumeConfiguration, respondCfg.Consume);
				context.Properties.Add(PipeKey.QueueDeclaration, respondCfg.Queue);
				context.Properties.Add(PipeKey.ExchangeDeclaration, respondCfg.Exchange);
			};
			return _client
				.RespondAsync(onMessage, ctxAction)
				.ContinueWith(tContext => tContext.Result.GetSubscription())
				.GetAwaiter()
				.GetResult();
		}

		public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = default(TRequest), Guid globalMessageId = new Guid(),
			Action<IRequestConfigurationBuilder> configuration = null)
		{
			var config = _configEval.GetConfiguration<TRequest, TResponse>(configuration);

			var exchangeCfg = config.Exchange.AssumeInitialized
				? null
				: new ExchangeDeclaration
				{
					AutoDelete = config.Exchange.AutoDelete,
					Durable = config.Exchange.Durable,
					ExchangeType = config.Exchange.ExchangeType,
					Name = config.Exchange.ExchangeName
				};
			var queueCfg = config.Queue.AssumeInitialized
				? null
				: new QueueDeclaration
				{
					Arguments = config.Queue.Arguments,
					Durable = config.Queue.Durable,
					AutoDelete = config.Queue.AutoDelete,
					Exclusive = config.Queue.Exclusive,
					Name = config.Queue.FullQueueName
				};
			var requestConfig = new Operations.Request.Configuration.RequestConfiguration
			{
				Request = new PublisherConfiguration
				{
					Exchange = exchangeCfg,
					ExchangeName = config.Exchange.ExchangeName,
					BasicProperties = new BasicProperties(),
					RoutingKey = config.RoutingKey,
					Mandatory = false
				},
				Response = new ConsumerConfiguration
				{
					Exchange = exchangeCfg,
					Queue = queueCfg,
					Consume = new ConsumeConfiguration
					{
						ExchangeName = config.Exchange.ExchangeName,
						ConsumerTag = Guid.NewGuid().ToString(),
						AutoAck = config.AutoAck,
						RoutingKey = config.ReplyQueueRoutingKey,
						QueueName = config.Queue.FullQueueName
					}
				}
			};
			Action<IPipeContext> ctxAction = context =>
			{
				context.Properties.TryAdd(RequestKey.Configuration, requestConfig);
				context.Properties.TryAdd(PipeKey.PublisherConfiguration, requestConfig.Request);
				context.Properties.TryAdd(PipeKey.ConsumerConfiguration, requestConfig.Response);
				context.Properties.TryAdd(PipeKey.ConsumeConfiguration, requestConfig.Response.Consume);
			};
			return _client.RequestAsync<TRequest, TResponse>(message, ctxAction);
		}

		public void Dispose()
		{
			(_client as IDisposable)?.Dispose();
		}
	}
}