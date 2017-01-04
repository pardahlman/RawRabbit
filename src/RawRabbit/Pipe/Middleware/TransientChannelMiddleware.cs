using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class TransientChannelMiddleware : Middleware
	{
		private readonly IChannelFactory _channelFactory;
		private readonly ILogger _logger = LogManager.GetLogger<TransientChannelMiddleware>();

		public TransientChannelMiddleware(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			return _channelFactory
				.GetChannelAsync(token)
				.ContinueWith(tChannel =>
				{
					_logger.LogDebug($"Adding channel {tChannel.Result.ChannelNumber} to Execution Context.");
					context.Properties.Add(PipeKey.TransientChannel, tChannel.Result);
					return Next.InvokeAsync(context, token);
				}, token)
				.Unwrap();
		}
	}
}
