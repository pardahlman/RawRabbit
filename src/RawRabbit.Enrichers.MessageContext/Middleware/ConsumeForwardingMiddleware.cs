using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Enrichers.MessageContext.Dependencies;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.MessageContext.Middleware
{
	public class ConsumeForwardingMiddleware : StagedMiddleware
	{
		private readonly IMessageContextRepository _repo;

		public ConsumeForwardingMiddleware(IMessageContextRepository repo)
		{
			_repo = repo;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var messageContext = context.GetMessageContext();
			if (messageContext != null)
			{
				_repo.Set(messageContext);
			}
			return Next.InvokeAsync(context, token);
		}

		public override string StageMarker => "MessageContextDeserialized";
	}
}
