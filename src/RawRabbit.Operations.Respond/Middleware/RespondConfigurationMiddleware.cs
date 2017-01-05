using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class RespondConfigurationOptions
	{
		public Func<IPipeContext, Type> RequestTypeFunc { get; set; }
		public Func<IPipeContext, Type> ResponseTypeFunc { get; set; }
	}

	public class RespondConfigurationMiddleware : ConfigurationMiddlewareBase
	{
		private readonly IRespondConfigurationFactory _factory;
		private readonly Func<IPipeContext, Type> _requestTypeFunc;
		private readonly Func<IPipeContext, Type> _responseTypeFunc;

		public RespondConfigurationMiddleware(IConsumerConfigurationFactory consumerFactory, RespondConfigurationOptions options = null)
			: this(new RespondConfigurationFactory(consumerFactory), options) { }

		public RespondConfigurationMiddleware(IRespondConfigurationFactory factory, RespondConfigurationOptions options = null)
		{
			_factory = factory;
			_requestTypeFunc = options?.RequestTypeFunc ?? (context => context.GetRequestMessageType());
			_responseTypeFunc = options?.RequestTypeFunc ?? (context => context.GetResponseMessageType());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var requestType = _requestTypeFunc(context);
			var responseType = _responseTypeFunc(context);
			var action = context.Get<Action<IRespondConfigurationBuilder>>(PipeKey.ConfigurationAction);

			if (requestType == null)
				throw new ArgumentNullException(nameof(requestType));
			if (responseType == null)
				throw new ArgumentNullException(nameof(responseType));
			var defaultCfg = _factory.Create(requestType, responseType);
			InvokeExchangeActions(context, requestType, defaultCfg.Exchange);
			InvokeQueueActions(context, requestType, defaultCfg.Queue);
			InvokeConsumeActions(context, requestType, defaultCfg.Consume);
			defaultCfg.Consume.ExchangeName = defaultCfg.Exchange.Name;
			defaultCfg.Consume.QueueName = defaultCfg.Queue.Name;
			
			var builder = new RespondConfigurationBuilder(defaultCfg);
			action?.Invoke(builder);

			var respondCfg = builder.Config;
			context.Properties.Add(PipeKey.ConsumerConfiguration, respondCfg);
			context.Properties.Add(PipeKey.ConsumeConfiguration, respondCfg.Consume);
			context.Properties.Add(PipeKey.QueueDeclaration, respondCfg.Queue);
			context.Properties.Add(PipeKey.ExchangeDeclaration, respondCfg.Exchange);

			return Next.InvokeAsync(context, token);
		}
	}
}
