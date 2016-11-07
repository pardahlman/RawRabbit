using System;
using System.Threading.Tasks;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class CorrelationIdMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var corrId = context.GetCorrelationId();
			if (string.IsNullOrWhiteSpace(corrId))
			{
				corrId = Guid.NewGuid().ToString();
				context.Properties.Add(RequestKey.CorrelationId, corrId);
			}
			return Next.InvokeAsync(context);
		}
	}
}
