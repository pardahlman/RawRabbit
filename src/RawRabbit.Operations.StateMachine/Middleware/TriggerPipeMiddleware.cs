using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class TriggerPipeOptions
	{
		public Func<IPipeContext, Action<IPipeBuilder>> PipeActionFunc { get; set; }
		public Func<IPipeContext, Action<IPipeContext>> ContextActionFunc { get; set; }
	}

	public class TriggerPipeMiddleware : Pipe.Middleware.Middleware
	{
		protected IPipeBuilderFactory PipeBuilder;
		protected IPipeContextFactory ContextFactory;
		protected Func<IPipeContext, Action<IPipeContext>> ContextActionFunc;
		protected Func<IPipeContext, Action<IPipeBuilder>> ChildPipeFunc;

		public TriggerPipeMiddleware(IPipeBuilderFactory pipeBuilder, IPipeContextFactory contextFactory, TriggerPipeOptions options = null)
		{
			PipeBuilder = pipeBuilder;
			ContextFactory = contextFactory;
			ContextActionFunc = options?.ContextActionFunc ?? (context => context.GetContextAction());
			ChildPipeFunc = options?.PipeActionFunc ?? (context => context.GetPipeBuilderAction());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var childContext = CreateChildContext(context);
			var contextAction = GetPipeContextAction(context);
			contextAction?.Invoke(childContext);

			var pipeAction = GetPipeBuilderAction(context);
			var childPipe = BuildPipe(pipeAction);

			return childPipe
				.InvokeAsync(childContext, token)
				.ContinueWith(t => Next.InvokeAsync(context, token), token)
				.Unwrap();
		}

		private Pipe.Middleware.Middleware BuildPipe(Action<IPipeBuilder> pipeBuilderAction)
		{
			return PipeBuilder.Create(pipeBuilderAction);
		}

		protected virtual Action<IPipeBuilder> GetPipeBuilderAction(IPipeContext context)
		{
			return ChildPipeFunc(context);
		}

		protected virtual IPipeContext CreateChildContext(IPipeContext context)
		{
			return ContextFactory.CreateContext(context.Properties.ToArray());
		}

		protected virtual Action<IPipeContext> GetPipeContextAction(IPipeContext context)
		{
			return ContextActionFunc(context);
		}
	}
}
