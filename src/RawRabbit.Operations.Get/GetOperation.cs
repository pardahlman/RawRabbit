using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Configuration.Get;
using RawRabbit.Operations.Get;
using RawRabbit.Operations.Get.Middleware;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class GetOperation
	{
		public static readonly Action<IPipeBuilder> UntypedGetPipe = pipe => pipe
			.Use<GetConfigurationMiddleware>()
			.Use<ChannelCreationMiddleware>()
			.Use<BasicGetMiddleware>()
			.Use<AckableResultMiddleware>(new AckableResultOptions
			{
				DeliveryTagFunc = context => context.GetBasicGetResult().DeliveryTag,
				ContentFunc = context => context.GetBasicGetResult()
			});

		public static async Task<Ackable<BasicGetResult>> GetAsync(this IBusClient busClient, Action<IGetConfigurationBuilder> config = null, CancellationToken token = default(CancellationToken))
		{
			var result = await busClient
				.InvokeAsync(UntypedGetPipe, context => context.Properties.Add(PipeKey.ConfigurationAction, config), token);
			return result.Get<Ackable<object>>(GetKey.AckableResult).AsAckable<BasicGetResult>();
		}
	}
}
