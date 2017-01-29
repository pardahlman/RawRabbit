using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class AddContextPropertyOptions
	{
		public Func<IPipeContext, string> KeyFunc { get; set; }
		public Func<IPipeContext, object> ValueFunc { get; set; }
		public Func<IPipeContext, bool> OverrideExistingFunc { get; set; }
	}

	public class AddContextPropertyMiddleware : Middleware
	{
		protected Func<IPipeContext, string> KeyFunc;
		protected Func<IPipeContext, object> ValueFunc;
		protected Func<IPipeContext, bool> OverrideExistingFunc;
		private readonly ILogger _logger = LogManager.GetLogger<AddContextPropertyMiddleware>();

		public AddContextPropertyMiddleware(AddContextPropertyOptions options)
		{
			KeyFunc = options?.KeyFunc;
			ValueFunc = options?.ValueFunc;
			OverrideExistingFunc = options?.OverrideExistingFunc ?? (context => true);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var key = GetKey(context);
			if (string.IsNullOrEmpty(key))
			{
				_logger.LogInformation("Key not found");
				return Next.InvokeAsync(context, token);
			}
			_logger.LogDebug($"Preparing to append '{key}' to Execution Context.");
			var value = GetValue(context);
			if (value == null)
			{
				_logger.LogInformation($"Value not found for key '{key}'");
				return Next.InvokeAsync(context, token);
			}
			if (!context.Properties.ContainsKey(key))
			{
				_logger.LogDebug("Context does not contain {key}.");
				context.Properties.Add(key, value);
				return Next.InvokeAsync(context, token);
			}
			var overrideExisting = OverrideExisting(context);
			if (overrideExisting)
			{
				_logger.LogInformation($"Context contains key {key}. Removing key.");
				context.Properties[key] = value;
			}

			return Next.InvokeAsync(context, token);
		}

		protected virtual bool OverrideExisting(IPipeContext context)
		{
			if (OverrideExistingFunc == null)
			{
				_logger.LogInformation("Override Existing Func not set. Defaulting to false.");
				return false;
			}
			return OverrideExistingFunc.Invoke(context);
		}

		protected virtual string GetKey(IPipeContext context)
		{
			return KeyFunc?.Invoke(context);
		}

		protected virtual object GetValue(IPipeContext context)
		{
			return ValueFunc?.Invoke(context);
		}
	}
}
