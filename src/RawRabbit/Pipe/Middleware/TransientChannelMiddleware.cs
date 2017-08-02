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
		private readonly ILog _logger = LogProvider.For<TransientChannelMiddleware>();

		public TransientChannelMiddleware(IChannelFactory channelFactory)
		{
			ChannelFactory = channelFactory;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var channel = await CreateChannelAsync(context, token);
			_logger.Debug("Adding channel {channelNumber} to Execution Context.", channel.ChannelNumber);
			context.Properties.Add(PipeKey.TransientChannel, channel);
			await Next.InvokeAsync(context, token);
		}

		protected virtual Task<IModel> CreateChannelAsync(IPipeContext context, CancellationToken ct)
		{
			return ChannelFactory.GetChannelAsync(ct);
		}
	}
}
