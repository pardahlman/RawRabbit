using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class StageMarkerMiddleware : Middleware
	{
		public readonly string Stage;
		private readonly Middleware _pipe;
		private readonly ILogger _logger = LogManager.GetLogger<StageMarkerMiddleware>();

		public StageMarkerMiddleware(StageMarkerOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			Stage = options.Stage;
			_pipe = options.EntryPoint;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			if (_pipe is NoOpMiddleware)
			{
				_logger.LogDebug($"Stage '{Stage}' has no additional middlewares registered.");
				return Next.InvokeAsync(context, token);
			}

			_logger.LogInformation($"Invoking additional middlewares on stage '{Stage}'.");
			return _pipe
				.InvokeAsync(context, token)
				.ContinueWith(t => Next.InvokeAsync(context, token), token)
				.Unwrap();
		}
	}

	public class StageMarkerOptions
	{
		public string Stage { get; set; }
		public Middleware EntryPoint { get; set; }

		public static StageMarkerOptions For<TPipe>(TPipe stage)
		{
			return new StageMarkerOptions
			{
				Stage = stage.ToString()
			};
		}
	}


	public abstract class StagedMiddleware : Middleware
	{
		public abstract string StageMarker { get; }
	}
}
