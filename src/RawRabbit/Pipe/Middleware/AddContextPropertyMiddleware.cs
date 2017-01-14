using System;
using System.Threading;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class AddContextPropertyOptions
	{
		public Func<IPipeContext, string> KeyFunc { get; set; }
		public Func<IPipeContext, object> ValueFunc { get; set; }
	}

	public class AddContextPropertyMiddleware : Middleware
	{
		protected Func<IPipeContext, string> KeyFunc;
		protected Func<IPipeContext, object> ValueFunc;

		public AddContextPropertyMiddleware(AddContextPropertyOptions options = null)
		{
			KeyFunc = options?.KeyFunc;
			ValueFunc = options?.ValueFunc;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var key = GetKey(context);
			if (string.IsNullOrEmpty(key))
			{
				return Next.InvokeAsync(context, token);
			}
			var value = GetValue(context);
			if (value == null)
			{
				return Next.InvokeAsync(context, token);
			}
			context.Properties.TryAdd(key, value);
			return Next.InvokeAsync(context, token);
		}

		protected virtual string GetKey(IPipeContext context)
		{
			return KeyFunc(context);
		}

		protected virtual object GetValue(IPipeContext context)
		{
			return ValueFunc(context);
		}
	}
}
