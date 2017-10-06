using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Context
{
	public interface IRequestContext : IPipeContext { }

	public class RequestContext : PipeContext, IRequestContext
	{
		public RequestContext(IPipeContext context)
		{
			Properties = context?.Properties;
		}
	}
}
