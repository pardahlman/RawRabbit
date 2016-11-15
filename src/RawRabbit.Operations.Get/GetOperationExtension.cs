using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Operations.Get.Middleware;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Get
{
	public static class GetOperationExtension
	{
		public static Action<IPipeBuilder> UntypedGetPipe = builder => builder
			.Use<ConsumeConfigurationMiddleware>()
			.Use<ChannelCreationMiddleware>()
			.Use<BasicGetMiddleware>()
			.Use<AckableGetResultMiddleware>();

		public static Task<AckableGetResult> GetAsync(this IBusClient busClient, Action<IConsumeConfigurationBuilder> config, CancellationToken token = default(CancellationToken))
		{
			return busClient
				.InvokeAsync(UntypedGetPipe, context => context.Properties.Add(PipeKey.ConfigurationAction, config), token)
				.ContinueWith(tContext => tContext.Result.Get<AckableGetResult>(GetKeys.AckableGetResult), token);
		}
	}
}
