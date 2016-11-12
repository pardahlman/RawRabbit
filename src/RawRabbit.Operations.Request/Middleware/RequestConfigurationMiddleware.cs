using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Operations.Request.Configuration;
using RawRabbit.Operations.Request.Configuration.Abstraction;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class RequestConfigurationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IRequestConfigurationFactory _factory;

		public RequestConfigurationMiddleware(IPublishConfigurationFactory publish, IConsumeConfigurationFactory consume)
		{
			_factory = new RequestConfigurationFactory(publish, consume);
		}

		public RequestConfigurationMiddleware(IRequestConfigurationFactory factory)
		{
			_factory = factory;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var requestType = context.GetRequestMessageType();
			var responseType = context.GetResponseMessageType();

			if (requestType == null)
			{
				throw new ArgumentNullException(nameof(requestType));
			}
			if (responseType == null)
			{
				throw new ArgumentNullException(nameof(responseType));
			}

			var defaultCfg = _factory.Create(requestType, responseType);
			var builder = new RequestConfigurationBuilder(defaultCfg);

			var action = context.Get<Action<IRequestConfigurationBuilder>>(PipeKey.ConfigurationAction);
			action?.Invoke(builder);
			var requestConfig = builder.Config;

			context.Properties.Add(RequestKey.Configuration, requestConfig);
			context.Properties.Add(PipeKey.PublishConfiguration, requestConfig.Request);
			context.Properties.Add(PipeKey.ConsumerConfiguration, requestConfig.Response);
			return Next.InvokeAsync(context);
		}
	}
}
