using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class RespondPublicationMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			throw new System.NotImplementedException();
		}
	}
}
