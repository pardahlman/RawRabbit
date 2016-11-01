using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Operations.Respond.Extensions;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ConsumeConfigurationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IConfigurationEvaluator _configEvaluator;

		public ConsumeConfigurationMiddleware(IConfigurationEvaluator configEvaluator)
		{
			_configEvaluator = configEvaluator;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var requestType = context.GetRequestMessageType();
			var responseType = context.GetResponseMessageType();
			var action = context.Get<Action<IResponderConfigurationBuilder>>(PipeKey.ConfigurationAction);

			if (requestType == null)
			{
				throw new ArgumentNullException(nameof(requestType));
			}
			if (responseType == null)
			{
				throw new ArgumentNullException(nameof(responseType));
			}

			var cfg = _configEvaluator.GetConfiguration(requestType, responseType, action);

			context.Properties.Add(PipeKey.QueueConfiguration, cfg.Queue);
			context.Properties.Add(PipeKey.ExchangeConfiguration, cfg.Exchange);
			context.Properties.Add(PipeKey.NoAck, cfg.NoAck);
			context.Properties.Add(PipeKey.PrefetchCount, cfg.PrefetchCount);
			context.Properties.Add(PipeKey.RoutingKey, cfg.RoutingKey);

			return Next.InvokeAsync(context);
		}
	}
}
