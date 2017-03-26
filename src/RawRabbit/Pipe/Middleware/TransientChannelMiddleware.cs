using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class TransientChannelMiddleware : Middleware
	{
		protected readonly IChannelFactory ChannelFactory;
		private readonly ILogger _logger = LogManager.GetLogger<TransientChannelMiddleware>();

		public TransientChannelMiddleware(IChannelFactory channelFactory)
		{
			ChannelFactory = channelFactory;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var channel = await CreateChannelAsync(context, token);
			_logger.LogDebug($"Adding channel {channel.ChannelNumber} to Execution Context.");
			context.Properties.Add(PipeKey.TransientChannel, channel);
			await Next.InvokeAsync(context, token);
		}

		protected virtual Task<IModel> CreateChannelAsync(IPipeContext context, CancellationToken ct)
		{
			return ChannelFactory.GetChannelAsync(ct);
		}
	}
}
