using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Pipe.Middleware
{
	public class CustomQueueSuffixOptions
	{
		public Func<IPipeContext, List<Action<IQueueDeclarationBuilder>>> QueueActionListFunc { get; set; }
		public Func<IPipeContext, Action<IQueueDeclarationBuilder>> QueueSuffixFunc { get; set; }
		public Func<IPipeContext, bool> ActivatedFlagFunc { get; set; }
	}

	public class CustomQueueSuffixMiddleware : Middleware
	{
		protected Func<IPipeContext, List<Action<IQueueDeclarationBuilder>>> QueueActionListFunc;
		protected Func<IPipeContext, Action<IQueueDeclarationBuilder>> QueueSuffixFunc;
		protected Func<IPipeContext, bool> ActivatedFlagFunc;

		public CustomQueueSuffixMiddleware(CustomQueueSuffixOptions options = null)
		{
			QueueActionListFunc = options?.QueueActionListFunc ?? (context => context.GetQueueActions());
			QueueSuffixFunc = options?.QueueSuffixFunc;
			ActivatedFlagFunc = options?.ActivatedFlagFunc ?? (context => context.GetCustomQueueSuffix() != null);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var activated = GetActivatedFlag(context);
			if (!activated)
			{
				return Next.InvokeAsync(context, token);
			}
			var queueActions = GetQueueActionList(context);
			var suffixAction = GetQueueSuffixAction(context);
			AddSuffixAction(queueActions, suffixAction);
			return Next.InvokeAsync(context, token);
		}

		protected virtual bool GetActivatedFlag(IPipeContext context)
		{
			return ActivatedFlagFunc.Invoke(context);
		}

		protected virtual List<Action<IQueueDeclarationBuilder>> GetQueueActionList(IPipeContext context)
		{
			return QueueActionListFunc(context);
		}

		protected virtual Action<IQueueDeclarationBuilder> GetQueueSuffixAction(IPipeContext context)
		{
			if (QueueSuffixFunc != null)
			{
				return QueueSuffixFunc.Invoke(context);
			}
			return builder =>
			{
				builder.WithNameSuffix(context.GetCustomQueueSuffix());
			};
		}

		protected virtual void AddSuffixAction(List<Action<IQueueDeclarationBuilder>> queueActions, Action<IQueueDeclarationBuilder> suffixAction)
		{
			queueActions.Add(suffixAction);
		}
	}

	public static class CustomSuffixExtension
	{
		private const string CustomQueueSuffix = "CustomQueueSuffix";

		public static IPipeContext UseCustomQueueSuffix(this IPipeContext context, string prefix)
		{
			context.Properties.TryAdd(CustomQueueSuffix, prefix);
			return context;
		}

		public static string GetCustomQueueSuffix(this IPipeContext context)
		{
			return context.Get<string>(CustomQueueSuffix);
		}
	}
}
