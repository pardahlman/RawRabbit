using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Advanced
{
	public static class CreateChannelExtension
	{
		public static readonly Action<IPipeBuilder> CreateChannelPipe = pipe => pipe
			.Use<ChannelCreationMiddleware>();

		public static Task<IModel> CreateChannelAsync(this IBusClient busClient, ChannelCreationOptions options = null, CancellationToken token = default(CancellationToken))
		{
			return busClient
				.InvokeAsync(CreateChannelPipe, token: token)
				.ContinueWith(tContext => tContext.Result.GetChannel(), token);
		}
	}
}
