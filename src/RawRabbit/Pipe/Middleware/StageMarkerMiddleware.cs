using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Logging;

namespace RawRabbit.Pipe.Middleware
{
	public class StageMarkerMiddleware : Middleware
	{
		public readonly string Stage;
		private readonly ILogger _logger = LogManager.GetLogger<StageMarkerMiddleware>();

		public StageMarkerMiddleware(StageMarkerOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			Stage = options.Stage;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			if (Next is NoOpMiddleware || Next is CancellationMiddleware)
			{
				_logger.LogDebug($"Stage '{Stage}' has no additional middlewares registered.");
			}
			else
			{
				_logger.LogInformation($"Invoking additional middlewares on stage '{Stage}'.");
			}
			await Next.InvokeAsync(context, token);
		}
	}

	public class StageMarkerOptions
	{
		public string Stage { get; set; }

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
