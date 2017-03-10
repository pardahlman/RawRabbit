using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.BasicPublish;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class BasicPublishExtension
	{
		public static readonly Action<IPipeBuilder> PublishPipe = builder => builder
			.Use<BasicPublishConfigurationMiddleware>()
			.Use<TransientChannelMiddleware>()
			.Use<BasicPublishMiddleware>();

		public static Task BasicPublishAsync(
				this IBusClient busClient,
				object message,
				Action<IBasicPublishConfigurationBuilder> config = null,
				CancellationToken token = default(CancellationToken))
		{
			return busClient.InvokeAsync(
				PublishPipe,
				context =>
				{
					context.Properties.Add(PipeKey.Message, message);
					context.Properties.Add(PipeKey.ConfigurationAction, config);
				},
				token
			);
		}

		public static Task BasicPublishAsync(
			this IBusClient busClient,
			BasicPublishConfiguration config,
			CancellationToken token = default(CancellationToken)
		)
		{
			return busClient.InvokeAsync(
				PublishPipe,
				context => context.Properties.Add(PipeKey.BasicPublishConfiguration, config),
				token
			);
		}
	}
}