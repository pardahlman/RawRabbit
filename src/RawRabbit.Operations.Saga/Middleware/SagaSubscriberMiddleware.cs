using System;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga.Middleware
{
	public class SagaSubscriberOptions
	{
		public Func<IPipeContext, Action<IPipeBuilder>> PipeActionFunc { get; set; }
		public Func<IPipeContext, Action<IPipeContext>> ContextActionFunc { get; set; }
	}

	public class SagaSubscriberMiddleware : Pipe.Middleware.Middleware
	{
		protected IPipeBuilderFactory PipeBuilder;
		protected IPipeContextFactory ContextFactory;
		protected Func<IPipeContext, Action<IPipeContext>> ContextActionFunc;
		protected Func<IPipeContext, Action<IPipeBuilder>> PipeActionFunc;

		public SagaSubscriberMiddleware(IPipeBuilderFactory pipeBuilder, IPipeContextFactory contextFactory, SagaSubscriberOptions options = null)
		{
			PipeBuilder = pipeBuilder;
			ContextFactory = contextFactory;
			ContextActionFunc = options?.ContextActionFunc ?? (context => context.GetContextAction());
			PipeActionFunc = options?.PipeActionFunc ?? (context => context.GetPipeBuilderAction());
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var childContext = CreateChildContext(context);
			var contextAction = GetPipeContextAction(context);
			contextAction?.Invoke(childContext);

			var pipeBuilderAction = GetPipeBuilderAction(context);
			var childPipe = BuildPipe(pipeBuilderAction);

			return childPipe
				.InvokeAsync(childContext)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}

		private Pipe.Middleware.Middleware BuildPipe(Action<IPipeBuilder> pipeBuilderAction)
		{
			return PipeBuilder.Create(pipeBuilderAction);
		}

		protected virtual Action<IPipeBuilder> GetPipeBuilderAction(IPipeContext context)
		{
			return PipeActionFunc(context);
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
