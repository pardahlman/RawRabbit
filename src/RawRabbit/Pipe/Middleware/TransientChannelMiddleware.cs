using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Pipe.Middleware
{
	public class TransientChannelMiddleware : Middleware
	{
		private readonly IChannelFactory _channelFactory;

		public TransientChannelMiddleware(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			return _channelFactory
				.GetChannelAsync()
				.ContinueWith(tChannel =>
				{
					context.Properties.Add(PipeKey.TransientChannel, tChannel.Result);
					return Next.InvokeAsync(context);
				})
				.Unwrap();
		}
	}
}
