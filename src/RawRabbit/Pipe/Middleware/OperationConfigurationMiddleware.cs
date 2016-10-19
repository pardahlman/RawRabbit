using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Configuration.Respond;
using RawRabbit.Configuration.Subscribe;

namespace RawRabbit.Pipe.Middleware
{
	public class OperationConfigurationMiddleware : Middleware
	{
		private readonly IConfigurationEvaluator _configEvaluator;

		public OperationConfigurationMiddleware(IConfigurationEvaluator configEvaluator)
		{
			_configEvaluator = configEvaluator;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var operation = context.GetOperation();
			var msgType = context.GetMessageType();

			switch (operation)
			{
				case Operation.Subscribe:
					{
						var action = context.Get<Action<ISubscriptionConfigurationBuilder>>(PipeKey.ConfigurationAction);
						IConsumerConfiguration cfg = _configEvaluator.GetConfiguration(msgType, action);
						context.Properties.Add(PipeKey.QueueConfiguration, cfg.Queue);
						context.Properties.Add(PipeKey.ExchangeConfiguration, cfg.Exchange);
						context.Properties.Add(PipeKey.NoAck, cfg.NoAck);
						context.Properties.Add(PipeKey.PrefetchCount, cfg.PrefetchCount);
					}
					break;

				case Operation.Publish:
					{
						var action = context.Get<Action<IPublishConfigurationBuilder>>(PipeKey.ConfigurationAction);
						var cfg = _configEvaluator.GetConfiguration(msgType, action);
						context.Properties.Add(PipeKey.ExchangeConfiguration, cfg.Exchange);
						context.Properties.Add(PipeKey.BasicPropertyModifier, cfg.PropertyModifier);
					}
					break;
				default:
					break;
			}

			context.Properties.Remove(PipeKey.ConfigurationAction);
			return Next.InvokeAsync(context);
		}
	}
}
