using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.Request.Core;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Request.Middleware
{
	public class RequestTimeoutOptions
	{
		public Func<IPipeContext, TimeSpan> TimeSpanFunc { get; set; }
	}

	public class RequestTimeoutMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, TimeSpan> TimeSpanFunc;

		public RequestTimeoutMiddleware(RequestTimeoutOptions options = null)
		{
			TimeSpanFunc = options?.TimeSpanFunc ?? (context => context.GetRequestTimeout());
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			if (token != default(CancellationToken))
			{
				await Next.InvokeAsync(context, token);
				return;
			}

			var timeout = GetTimeoutTimeSpan(context);
			var ctc = new CancellationTokenSource(timeout);
			var timeoutTsc = new TaskCompletionSource<bool>();

			ctc.Token.Register(() =>
			{
				var correlationId = context?.GetBasicProperties()?.CorrelationId;
				var cfg = context?.GetRequestConfiguration();
				timeoutTsc.TrySetException(new TimeoutException($"The request '{correlationId}' with routing key '{cfg?.Request.RoutingKey}' timed out after {timeout:g}."));
			});

			var pipeTask = Next
				.InvokeAsync(context, ctc.Token)
				.ContinueWith(t =>
				{
					if (!ctc.IsCancellationRequested)
					{
						timeoutTsc.TrySetResult(true);
					}
					return t;
				}, token)
				.Unwrap();

			await timeoutTsc.Task;
			await pipeTask;
			ctc.Dispose();
		}

		protected virtual TimeSpan GetTimeoutTimeSpan(IPipeContext context)
		{
			return TimeSpanFunc(context);
		}
	}

	public static class RequestTimeoutExtensions
	{
		private const string RequestTimeout = "RequestTimeout";

		public static IPipeContext UseRequestTimeout(this IPipeContext context, TimeSpan time)
		{
			context.Properties.TryAdd(RequestTimeout, time);
			return context;
		}

		public static TimeSpan GetRequestTimeout(this IPipeContext context)
		{
			var fallback = context.GetClientConfiguration().RequestTimeout;
			return context.Get(RequestTimeout, fallback);
		}
	}
}
