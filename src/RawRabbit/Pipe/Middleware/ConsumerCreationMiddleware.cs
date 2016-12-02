using System;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Consumer;
using RawRabbit.Consumer;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumerCreationOptions
	{
		public Func<IPipeContext, ConsumeConfiguration> ConsumeFunc { get; set; }
	}

	public class ConsumerCreationMiddleware : Middleware
	{
		private readonly IConsumerFactory _consumerFactory;
		private readonly Func<IPipeContext, ConsumeConfiguration> _consumeCfgFunc;

		public ConsumerCreationMiddleware(IConsumerFactory consumerFactory, ConsumerCreationOptions options = null)
		{
			_consumerFactory = consumerFactory;
			_consumeCfgFunc = options?.ConsumeFunc ?? (context => context.GetConsumeConfiguration());

		}
		public override Task InvokeAsync(IPipeContext context)
		{
			var consumeCfg = _consumeCfgFunc(context);
			return _consumerFactory
				.CreateConsumerAsync(consumeCfg)
				.ContinueWith(tConsumer =>
				{
					context.Properties.Add(PipeKey.Consumer, tConsumer.Result);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}
	}
}
