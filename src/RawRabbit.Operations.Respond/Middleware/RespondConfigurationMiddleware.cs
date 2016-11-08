using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class RespondConfigurationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IRespondConfigurationFactory _factory;

		public RespondConfigurationMiddleware(IRespondConfigurationFactory factory)
		{
			_factory = factory;
		}

		public RespondConfigurationMiddleware(IConsumeConfigurationFactory consumeFactory)
		{
			_factory = new RespondConfigurationFactory(consumeFactory);
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var requestType = context.GetRequestMessageType();
			var responseType = context.GetResponseMessageType();
			var action = context.Get<Action<IRespondConfigurationBuilder>>(PipeKey.ConfigurationAction);

			if (requestType == null)
			{
				throw new ArgumentNullException(nameof(requestType));
			}
			if (responseType == null)
			{
				throw new ArgumentNullException(nameof(responseType));
			}

			var defaultCfg = _factory.Create(requestType, responseType);
			var builder = new RespondConfigurationBuilder(defaultCfg);
			action?.Invoke(builder);

			var respondCfg = builder.Config;
			context.Properties.Add(RespondKey.Configuration, respondCfg);

			return Next.InvokeAsync(context);
		}
	}
}
