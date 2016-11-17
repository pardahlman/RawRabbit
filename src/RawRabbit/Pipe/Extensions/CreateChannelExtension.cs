using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Pipe.Extensions
{
	public static class CreateChannelExtension
	{
		public static readonly Action<IPipeBuilder> CreateChannelPipe = pipe => pipe
			.Use<ChannelCreationMiddleware>();

		public static Task<IModel> CreateChannelAsync(this IBusClient busClient)
		{
			return busClient.InvokeAsync(CreateChannelPipe).ContinueWith(tContext => tContext.Result.GetChannel());
		}
	}
}
