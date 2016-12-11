using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Consume;
using RawRabbit.Consumer;

namespace RawRabbit.Pipe.Middleware
{
	public class ConsumerOptions
	{
		public Func<IPipeContext, ConsumeConfiguration> ConfigurationFunc { get; set; }
		public Func<IConsumerFactory, IPipeContext, Task<IBasicConsumer>> ConsumerFunc { get; set; }
	}

	public class ConsumerMiddleware : Middleware
	{
		protected IConsumerFactory ConsumerFactory;
		protected Func<IPipeContext, ConsumeConfiguration> ConfigFunc;
		protected Func<IConsumerFactory, IPipeContext, Task<IBasicConsumer>> ConsumerFunc;

		public ConsumerMiddleware(IConsumerFactory consumerFactory, ConsumerOptions options = null)
		{
			ConsumerFactory = consumerFactory;
			ConfigFunc = options?.ConfigurationFunc ?? (context => context.GetConsumeConfiguration());
			ConsumerFunc = options?.ConsumerFunc ?? ((factory, context) => factory.CreateConsumerAsync());
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			return GetOrCreateConsumerAsync(context)
				.ContinueWith(tConsumer =>
					{
						var config = GetConfiguration(context);
						if (config != null)
						{
							ConsumerFactory.ConfigureConsume(tConsumer.Result, config);
						}
						context.Properties.TryAdd(PipeKey.Consumer, tConsumer.Result);
						return Next.InvokeAsync(context);
					})
				.Unwrap();
		}

		protected virtual ConsumeConfiguration GetConfiguration(IPipeContext context)
		{
			return ConfigFunc(context);
		}

		protected virtual Task<IBasicConsumer> GetOrCreateConsumerAsync(IPipeContext context)
		{
			return ConsumerFunc(ConsumerFactory, context);
		}
	}
}
