using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Pipe.Middleware
{
	public class PublishChannelMiddleware : Middleware
	{
		private readonly IChannelFactory _channelFactory;

		public PublishChannelMiddleware(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			return _channelFactory
				.GetChannelAsync()
				.ContinueWith(async tChannel =>
				{
					context.Properties.Add(PipeKey.Channel, tChannel.Result);
					await Next.InvokeAsync(context);
				});
		}
	}
}
