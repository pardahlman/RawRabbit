using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Middleware
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
			var messageType = context.GetMessageType();
			var action = context.Get<Action<ISubscriptionConfigurationBuilder>>(PipeKey.ConfigurationAction);

			if (messageType == null)
			{
				throw new ArgumentNullException(nameof(messageType));
			}

			IConsumerConfiguration cfg = _configEvaluator.GetConfiguration(messageType, action);

			context.Properties.Add(PipeKey.QueueConfiguration, cfg.Queue);
			context.Properties.Add(PipeKey.ExchangeConfiguration, cfg.Exchange);
			context.Properties.Add(PipeKey.NoAck, cfg.NoAck);
			context.Properties.Add(PipeKey.PrefetchCount, cfg.PrefetchCount);
			context.Properties.Add(PipeKey.RoutingKey, cfg.RoutingKey);

			return Next.InvokeAsync(context);
		}
	}
}
