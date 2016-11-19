using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class ConsumeTriggerMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var triggerCfg = context.Get<TriggerConfiguration>(SagaKey.TriggerConfiguration);
			var triggers = triggerCfg.ConfigureTriggers();
			context.Properties.Add(SagaKey.ExternalTriggers, triggers);
			return Next.InvokeAsync(context);
		}
	}
}
