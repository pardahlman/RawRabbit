using System;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class StageMarkerMiddleware<TPipe> : Middleware
	{
		private readonly Middleware _pipe;
		public TPipe StageMarker { get; }

		public StageMarkerMiddleware(StageMarkerOptions<TPipe> options, IPipeBuilderFactory builder)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			StageMarker = options.StageMarker;
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

	public class StageMarkerMiddleware : StageMarkerMiddleware<StageMarker>
	{
		public StageMarkerMiddleware(StageMarkerOptions options, IPipeBuilderFactory builder) : base(options, builder)
		{
		}
	}

	public class StageMarkerOptions : StageMarkerOptions<StageMarker>
	{
		public new static StageMarkerOptions For(StageMarker stage)
		{
			return new StageMarkerOptions
			{
				StageMarker = stage
			};
		}
	}

	public class StageMarkerOptions<TPipe>
	{
		public TPipe StageMarker { get; set; }
		public Middleware EntryPoint { get; set; }

		public static StageMarkerOptions<TPipe> For(TPipe stage)
		{
			return new StageMarkerOptions<TPipe>
			{
				StageMarker = stage
			};
		}
	}

	public abstract class StagedMiddleware : Middleware
	{
		public abstract StageMarker StageMarker { get; }
	}

	public enum StageMarker
	{
		NotSpecified,
		PreChannelCreation,
		PostChannelCreation,
		PreMessagePublish,
		PostMessagePublish,
		Recieved
	}
}
