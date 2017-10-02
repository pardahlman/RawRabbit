using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.Polly.Middleware
{
	public class TransientChannelMiddleware : Pipe.Middleware.TransientChannelMiddleware
	{
		public TransientChannelMiddleware(IChannelFactory channelFactory)
			: base(channelFactory) { }

		protected override Task<IModel> CreateChannelAsync(IPipeContext context, CancellationToken token)
		{
			var policy = context.GetPolicy(PolicyKeys.ChannelCreate);
			return policy.ExecuteAsync(
				action: ct => base.CreateChannelAsync(context, ct),
				cancellationToken: token,
				contextData: new Dictionary<string, object>
				{
					[RetryKey.PipeContext] = context,
					[RetryKey.CancellationToken] = token,
					[RetryKey.PipeContext] = ChannelFactory,
				}
			);
		}
	}
}
