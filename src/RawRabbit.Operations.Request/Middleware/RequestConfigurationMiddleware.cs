using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Publisher;
using RawRabbit.Configuration.Queue;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Configuration.Abstraction;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Request.Middleware
{
	public class RequestConfigurationMiddleware : ConfigurationMiddlewareBase
	{
		private readonly IRequestConfigurationFactory _factory;

		public RequestConfigurationMiddleware(IPublisherConfigurationFactory publisher, IConsumerConfigurationFactory consumer)
		{
			_factory = new RequestConfigurationFactory(publisher, consumer);
		}

		public RequestConfigurationMiddleware(IRequestConfigurationFactory factory)
		{
			_factory = factory;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var requestType = context.GetRequestMessageType();
			var responseType = context.GetResponseMessageType();

			if (requestType == null)
				throw new ArgumentNullException(nameof(requestType));
			if (responseType == null)
				throw new ArgumentNullException(nameof(responseType));

			var defaultCfg = _factory.Create(requestType, responseType);

			InvokeQueueActions(context, responseType, defaultCfg.Response.Queue);
			InvokeExchangeActions(context, responseType, defaultCfg.Response.Exchange);
			InvokeConsumeActions(context, responseType, defaultCfg.Response.Consume);
			defaultCfg.Response.Consume.ExchangeName = defaultCfg.Response.Exchange.Name;
			defaultCfg.Response.Consume.QueueName = defaultCfg.Response.Queue.Name;

			InvokeExchangeActions(context, requestType, defaultCfg.Request.Exchange);
			InvokePublishActions(context, requestType, defaultCfg.Request);
			defaultCfg.Request.ExchangeName = defaultCfg.Request.Exchange.Name;

			var builder = new RequestConfigurationBuilder(defaultCfg);
			var action = context.Get<Action<IRequestConfigurationBuilder>>(PipeKey.ConfigurationAction);
			action?.Invoke(builder);
			var requestConfig = builder.Config;

			context.Properties.Add(RequestKey.Configuration, requestConfig);
			context.Properties.Add(PipeKey.PublisherConfiguration, requestConfig.Request);
			context.Properties.Add(PipeKey.ConsumerConfiguration, requestConfig.Response);
			context.Properties.Add(PipeKey.ConsumeConfiguration, requestConfig.Response.Consume);
			return Next.InvokeAsync(context, token);
		}
	}
}
