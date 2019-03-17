using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Logging;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using IModel = RabbitMQ.Client.IModel;

namespace RawRabbit.Operations.Subscribe.Middleware
{
	public class SubscriptionExceptionOptions
	{
		public Func<IPipeContext, IChannelFactory, Task<IModel>> ChannelFunc { get; set; }
		public Action<IPipeBuilder> InnerPipe { get; set; }
	}

	public class SubscriptionExceptionMiddleware : ExceptionHandlingMiddleware
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ITopologyProvider _provider;
		private readonly INamingConventions _conventions;
		private readonly ILog _logger = LogProvider.For<SubscriptionExceptionMiddleware>();
		protected Func<IPipeContext, IChannelFactory, Task<IModel>> ChannelFunc;

		public SubscriptionExceptionMiddleware(
			IPipeBuilderFactory factory,
			IChannelFactory channelFactory,
			ITopologyProvider provider,
			INamingConventions conventions,
			SubscriptionExceptionOptions options)
			: base(factory, new ExceptionHandlingOptions {InnerPipe = options.InnerPipe})
		{
			_channelFactory = channelFactory;
			_provider = provider;
			_conventions = conventions;
			ChannelFunc = options?.ChannelFunc ?? ((c, f) =>f.CreateChannelAsync());
		}

		protected override async Task OnExceptionAsync(Exception exception, IPipeContext context, CancellationToken token)
		{
			_logger.Info(exception, "Unhandled exception thrown when consuming message");
			try
			{
				var exchangeCfg = GetExchangeDeclaration(context);
				await DeclareErrorExchangeAsync(exchangeCfg);
				var channel = await GetChannelAsync(context);
				await PublishToErrorExchangeAsync(context, channel, exception, exchangeCfg);
				channel.Dispose();
			}
			catch (Exception e)
			{
				_logger.Error(e, "Unable to publish message to Error Exchange");
			}
			try
			{
				await AckMessageIfApplicable(context);
			}
			catch (Exception e)
			{
				_logger.Error(e, "Unable to ack message.");
			}
		}

		protected virtual Task<IModel> GetChannelAsync(IPipeContext context)
		{
			return ChannelFunc(context, _channelFactory);
		}

		protected virtual Task DeclareErrorExchangeAsync(ExchangeDeclaration exchange)
		{
			return _provider.DeclareExchangeAsync(exchange);
		}

		protected virtual ExchangeDeclaration GetExchangeDeclaration(IPipeContext context)
		{
			var generalCfg = context?.GetClientConfiguration()?.Exchange;
			return new ExchangeDeclaration(generalCfg)
			{
				Name = _conventions.ErrorExchangeNamingConvention()
			};
		}

		protected virtual Task PublishToErrorExchangeAsync(IPipeContext context, IModel channel, Exception exception, ExchangeDeclaration exchange)
		{
			var args = context.GetDeliveryEventArgs();
			args.BasicProperties.Headers?.TryAdd(PropertyHeaders.Host, Environment.MachineName);
			args.BasicProperties.Headers?.TryAdd(PropertyHeaders.ExceptionType, exception.GetType().Name);
			args.BasicProperties.Headers?.TryAdd(PropertyHeaders.ExceptionStackTrace, exception.StackTrace);
			channel.BasicPublish(exchange.Name, args.RoutingKey, false, args.BasicProperties, args.Body);
			return Task.FromResult(0);
		}

		protected virtual Task AckMessageIfApplicable(IPipeContext context)
		{
			var autoAck = context.GetConsumeConfiguration()?.AutoAck;
			if (!autoAck.HasValue)
			{
				_logger.Debug("Unable to ack original message. Can not determine if AutoAck is configured.");
				return Task.FromResult(0);
			}
			if (autoAck.Value)
			{
				_logger.Debug("Consuming in AutoAck mode. No ack'ing will be performed");
				return Task.FromResult(0);
			}
			var deliveryTag = context.GetDeliveryEventArgs()?.DeliveryTag;
			if (deliveryTag == null)
			{
				_logger.Info("Unable to ack original message. Delivery tag not found.");
				return Task.FromResult(0);
			}
			var consumerChannel = context.GetConsumer()?.Model;
			if (consumerChannel != null && consumerChannel.IsOpen && deliveryTag.HasValue)
			{
				_logger.Debug("Acking message with {deliveryTag} on channel {channelNumber}", deliveryTag, consumerChannel.ChannelNumber);
				consumerChannel.BasicAck(deliveryTag.Value, false);
			}
			return Task.FromResult(0);
		}
	}
}
