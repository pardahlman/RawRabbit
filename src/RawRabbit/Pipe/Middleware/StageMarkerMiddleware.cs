using System;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class StageMarkerMiddleware : Middleware
	{
		public readonly string Stage;
		private readonly Middleware _pipe;

		public StageMarkerMiddleware(StageMarkerOptions options)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			Stage = options.Stage;
			_pipe = options.EntryPoint;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			return _pipe
				.InvokeAsync(context)
				.ContinueWith(t => Next.InvokeAsync(context))
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
