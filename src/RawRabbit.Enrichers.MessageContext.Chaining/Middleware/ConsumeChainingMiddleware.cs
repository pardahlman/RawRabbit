using System.Threading.Tasks;
using RawRabbit.Enrichers.MessageContext.Chaining.Dependencies;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.MessageContext.Chaining.Middleware
{
	public class ConsumeChainingMiddleware : StagedMiddleware
	{
		private readonly IMessageContextRepository _repo;

		public ConsumeChainingMiddleware(IMessageContextRepository repo)
		{
			_repo = repo;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var messageContext = context.GetMessageContext();
			if (messageContext != null)
			{
				_repo.Set(messageContext);
			}
			return Next.InvokeAsync(context);
		}

		public override string StageMarker => "MessageContextDeserialized";
	}
}
