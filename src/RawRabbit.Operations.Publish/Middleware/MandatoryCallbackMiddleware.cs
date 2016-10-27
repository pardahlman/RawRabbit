using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class MandatoryCallbackMiddleware : Pipe.Middleware.Middleware
	{
		public override Task InvokeAsync(IPipeContext context)
		{
			var callback = context.GetReturnedMessageCallback();
			if (callback == null)
			{
				return Next.InvokeAsync(context);
			}

			var channel = context.GetChannel();
			if (channel == null)
			{
				return Next.InvokeAsync(context);
			}

			channel.BasicReturn += callback;
			return Next
				.InvokeAsync(context)
				.ContinueWith(t => channel.BasicReturn -= callback);
		}
	}
}
