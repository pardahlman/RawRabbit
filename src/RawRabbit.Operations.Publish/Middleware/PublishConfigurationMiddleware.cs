using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Publish;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class PublishConfigurationMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IConfigurationEvaluator _configEval;

		public PublishConfigurationMiddleware(IConfigurationEvaluator configEval)
		{
			_configEval = configEval;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var action = context.Get<Action<IPublishConfigurationBuilder>>(PipeKey.ConfigurationAction);
			var messageType = context.GetMessageType();
			
			if (messageType == null)
			{
				throw new KeyNotFoundException(PipeKey.MessageType);
			}

			var cfg = _configEval.GetConfiguration(messageType, action);

			context.Properties.Add(PipeKey.ExchangeConfiguration, cfg.Exchange);
			context.Properties.Add(PipeKey.BasicPropertyModifier, cfg.PropertyModifier);
			context.Properties.Add(PipeKey.ReturnedMessageCallback, cfg.ReturnCallback);
			return Next.InvokeAsync(context);
		}
	}
}
