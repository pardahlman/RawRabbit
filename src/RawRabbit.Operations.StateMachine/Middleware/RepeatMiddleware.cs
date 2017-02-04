using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.StateMachine.Middleware
{
	public class RepeatOptions
	{
		public Action<IPipeBuilder> RepeatePipe { get; set; }
		public Func<IPipeContext, IEnumerable> EnumerableFunc { get; set; }
		public Func<IPipeContext, IPipeContextFactory, object, IPipeContext> RepeatContextFactory { get; set; }
	}
	public class RepeatMiddleware : Pipe.Middleware.Middleware
	{
		private readonly IPipeContextFactory _contextFactory;
		protected Pipe.Middleware.Middleware RepeatPipe;
		protected Func<IPipeContext, IEnumerable> EnumerableFunc;
		protected Func<IPipeContext, IPipeContextFactory, object, IPipeContext> RepeateContextFactory;

		public RepeatMiddleware(IPipeBuilderFactory factroy, IPipeContextFactory contextFactory, RepeatOptions options)
		{
			RepeatPipe = factroy.Create(options.RepeatePipe);
			_contextFactory = contextFactory;
			EnumerableFunc = options.EnumerableFunc;
			RepeateContextFactory = options.RepeatContextFactory ?? ((context, factory, obj) => context);
		}
		
		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var childTasks = new List<Task>();
			var enumerable = EnumerableFunc(context);
			foreach (var enumerated in enumerable)
			{
				var childContext = RepeateContextFactory(context, _contextFactory, enumerated);
				var childTask = RepeatPipe.InvokeAsync(childContext, token);
				childTasks.Add(childTask);
			}
			return Task
				.WhenAll(childTasks)
				.ContinueWith(t => Next.InvokeAsync(context, token), token)
				.Unwrap();
		}
	}
}