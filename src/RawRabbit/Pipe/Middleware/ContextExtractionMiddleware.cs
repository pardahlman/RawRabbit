using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Context.Provider;

namespace RawRabbit.Pipe.Middleware
{
	public class ContextExtractionMiddleware<TMessageContext> : Middleware where TMessageContext : IMessageContext
	{
		private readonly IMessageContextProvider<TMessageContext> _contextProvider;

		public ContextExtractionMiddleware(IMessageContextProvider<TMessageContext> contextProvider)
		{
			_contextProvider = contextProvider;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var basicProperties = context.GetBasicProperties();
			var msgContext = _contextProvider.ExtractContext(basicProperties?.Headers?.GetOrDefault(PropertyHeaders.Context));
			context.Properties.Add(PipeKey.MessageContext, msgContext);
			return Next.InvokeAsync(context);
		}
	}
}