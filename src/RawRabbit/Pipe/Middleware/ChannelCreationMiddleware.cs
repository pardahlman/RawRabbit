using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Pipe.Middleware
{
	public class ChannelCreationMiddleware : Middleware
	{
		private readonly IChannelFactory _channelFactory;

		public ChannelCreationMiddleware(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			return _channelFactory
				.CreateChannelAsync()
				.ContinueWith(tChannel =>
				{
					context.Properties.TryAdd(PipeKey.Channel, tChannel.Result);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}
	}
}
