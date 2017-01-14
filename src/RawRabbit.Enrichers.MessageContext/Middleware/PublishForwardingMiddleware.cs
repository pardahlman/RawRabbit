using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Enrichers.MessageContext.Dependencies;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.MessageContext.Middleware
{
	public class PublishForwardingMiddleware : StagedMiddleware
	{
		private readonly IMessageContextRepository _repo;

		public PublishForwardingMiddleware(IMessageContextRepository repo)
		{
			_repo = repo;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var messageContext = _repo.Get();
			if (messageContext == null)
			{
				return Next.InvokeAsync(context, token);
			}
			if (context.Properties.ContainsKey(PipeKey.MessageContext))
			{
				context.Properties.Remove(PipeKey.MessageContext);
			}
			context.Properties.Add(PipeKey.MessageContext, messageContext);
			return Next.InvokeAsync(context, token);
		}

		public override string StageMarker => Pipe.StageMarker.Initialized;
	}
}
