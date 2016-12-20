using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel.Abstraction;

namespace RawRabbit.Pipe.Middleware
{
	public class ChannelCreationOptions
	{
		public Predicate<IPipeContext> CreatePredicate { get; set; }
		public Action<IPipeContext, IModel> PostExecuteAction { get; set; }
		public Func<IChannelFactory, CancellationToken, Task<IModel>> CreateFunc { get; set; }
	}

	public class ChannelCreationMiddleware : Middleware
	{
		protected readonly IChannelFactory ChannelFactory;
		protected Predicate<IPipeContext> CreatePredicate;
		protected Func<IChannelFactory, CancellationToken, Task<IModel>> CreateFunc;
		protected Action<IPipeContext, IModel> PostExecuteAction;

		public ChannelCreationMiddleware(IChannelFactory channelFactory, ChannelCreationOptions options = null)
		{
			ChannelFactory = channelFactory;
			CreatePredicate = options?.CreatePredicate ?? (context => !context.Properties.ContainsKey(PipeKey.Channel));
			CreateFunc = options?.CreateFunc ?? ((factory, token) => factory.CreateChannelAsync(token));
			PostExecuteAction = options?.PostExecuteAction;
		}
		public override Task InvokeAsync(IPipeContext context, CancellationToken token = default(CancellationToken))
		{
			if (!ShouldCreateChannel(context))
			{
				return Next.InvokeAsync(context, token);
			}

			return GetOrCreateChannelAsync(ChannelFactory, token)
				.ContinueWith(tChannel =>
				{
					context.Properties.TryAdd(PipeKey.Channel, tChannel.Result);
					PostExecuteAction?.Invoke(context, tChannel.Result);
					return Next.InvokeAsync(context, token);
				}, token)
				.Unwrap();
		}

		protected virtual Task<IModel> GetOrCreateChannelAsync(IChannelFactory factory, CancellationToken token)
		{
			return CreateFunc(factory, token);
		}

		protected virtual bool ShouldCreateChannel(IPipeContext context)
		{
			return CreatePredicate(context);
		}
	}
}
