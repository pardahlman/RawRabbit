using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class CreateChannelExtension
	{
		public static readonly Action<IPipeBuilder> CreateChannelPipe = pipe => pipe
			.Use<ChannelCreationMiddleware>();

		public static async Task<IModel> CreateChannelAsync(this IBusClient busClient, ChannelCreationOptions options = null, CancellationToken token = default(CancellationToken))
		{
			var context = await busClient.InvokeAsync(CreateChannelPipe, token: token);
			return context.GetChannel();
		}
	}
}
