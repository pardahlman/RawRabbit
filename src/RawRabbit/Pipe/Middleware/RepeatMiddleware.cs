using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RawRabbit.Pipe.Middleware
{
	public class RepeateOptions
	{
		public Action<IPipeBuilder> RepeatePipe { get; set; }
		public Func<IPipeContext, IEnumerable> EnumerableFunc { get; set; }
		public Func<IPipeContext, IPipeContextFactory, object, IPipeContext> RepeatContextFactory { get; set; }
	}
	public class RepeatMiddleware : Middleware
	{
		private readonly IPipeContextFactory _contextFactory;
		protected Middleware RepeatPipe;
		protected Func<IPipeContext, IEnumerable> EnumerableFunc;
		protected Func<IPipeContext, IPipeContextFactory, object, IPipeContext> RepeateContextFactory;

		public RepeatMiddleware(IPipeBuilderFactory factroy, IPipeContextFactory contextFactory, RepeateOptions options)
		{
			RepeatPipe = factroy.Create(options.RepeatePipe);
			_contextFactory = contextFactory;
			EnumerableFunc = options.EnumerableFunc;
			RepeateContextFactory = options.RepeatContextFactory ?? ((context, factory, obj) => context);
		}
		
		public override Task InvokeAsync(IPipeContext context)
		{
			var childTasks = new List<Task>();
			var enumerable = EnumerableFunc(context);
			foreach (var enumerated in enumerable)
			{
				var childContext = RepeateContextFactory(context, _contextFactory, enumerated);
				var childTask = RepeatPipe.InvokeAsync(childContext);
				childTasks.Add(childTask);
			}
			return Task
				.WhenAll(childTasks)
				.ContinueWith(t => Next.InvokeAsync(context))
				.Unwrap();
		}
	}
}