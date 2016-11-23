using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Request;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Context;
using RawRabbit.Logging;
using RawRabbit.Operations.Abstraction;

namespace RawRabbit.Common
{
    public class BaseBusClient<TMessageContext> : IBusClient<TMessageContext> where TMessageContext : IMessageContext
    {
        private readonly IConfigurationEvaluator _configEval;
        private readonly ISubscriber<TMessageContext> _subscriber;
        private readonly IPublisher _publisher;
        private readonly IResponder<TMessageContext> _responder;
        private readonly IRequester _requester;
        private readonly ILogger _logger = LogManager.GetLogger<BaseBusClient<TMessageContext>>();
            
        public BaseBusClient(
            IConfigurationEvaluator configEval,
            ISubscriber<TMessageContext> subscriber,
            IPublisher publisher,
            IResponder<TMessageContext> responder,
            IRequester requester)
        {
            _configEval = configEval;
            _subscriber = subscriber;
            _publisher = publisher;
            _responder = responder;
            _requester = requester;
            _logger.LogInformation("BusClient initialized.");
        }

        public ISubscription SubscribeAsync<T>(Func<T, TMessageContext, Task> subscribeMethod, Action<ISubscriptionConfigurationBuilder> configuration = null)
        {
            var config = _configEval.GetConfiguration<T>(configuration);
            _logger.LogInformation($"Subscribing to message '{typeof(T).Name}' on exchange '{config.Exchange.ExchangeName}' with routing key {config.RoutingKey}.");
            return _subscriber.SubscribeAsync(subscribeMethod, config);
        }

        public Task PublishAsync<T>(T message = default(T), Guid globalMessageId = new Guid(), Action<IPublishConfigurationBuilder> configuration = null)
        {
            var config = _configEval.GetConfiguration<T>(configuration);
            _logger.LogDebug($"Initiating publish for message '{typeof(T).Name}' on exchange '{config.Exchange.ExchangeName}' with routing key {config.RoutingKey}.");
            return _publisher.PublishAsync(message, globalMessageId, config);
        }

        public ISubscription RespondAsync<TRequest, TResponse>(Func<TRequest, TMessageContext, Task<TResponse>> onMessage, Action<IResponderConfigurationBuilder> configuration = null)
        {
            var config = _configEval.GetConfiguration<TRequest, TResponse>(configuration);
            _logger.LogInformation($"Responding to to requests '{typeof(TRequest).Name}' with '{typeof(TResponse).Name}'.");
            return _responder.RespondAsync(onMessage, config);
        }

        public Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest message = default(TRequest), Guid globalMessageId = new Guid(), Action<IRequestConfigurationBuilder> configuration = null)
        {
            var config = _configEval.GetConfiguration<TRequest, TResponse>(configuration);
            _logger.LogDebug($"Requsting message '{typeof(TRequest).Name}' on exchange '{config.Exchange.ExchangeName}' with routing key {config.RoutingKey}.");
            return _requester.RequestAsync<TRequest, TResponse>(message, globalMessageId, config);
        }

        public Task ShutdownAsync(TimeSpan? graceful = null)
        {
            _logger.LogDebug("Shutting Down BusClient and its dependencies.");

            var subTask = (_subscriber as IShutdown)?.ShutdownAsync(graceful) ?? Task.FromResult(true);
            var pubTask = (_publisher as IShutdown)?.ShutdownAsync(graceful) ?? Task.FromResult(true);
            var reqTask = (_requester as IShutdown)?.ShutdownAsync(graceful) ?? Task.FromResult(true);
            var respTask = (_responder as IShutdown)?.ShutdownAsync(graceful) ?? Task.FromResult(true);
            return Task.WhenAll(subTask, pubTask, reqTask, respTask);
        }
    }
}
