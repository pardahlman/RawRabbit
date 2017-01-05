using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Pipe.Middleware
{
	public class HostnameQueueSuffixOptions
	{
		public Func<IPipeContext, List<Action<IQueueDeclarationBuilder>>> QueueActionListFunc { get; set; }
		public Func<IPipeContext, Action<IQueueDeclarationBuilder>> QueueSuffixFunc { get; set; }
		public Func<IPipeContext, bool> ActivatedFlagFunc { get; set; }
	}

	public class HostnameQueueSuffixMiddleware : Middleware
	{
		protected Func<IPipeContext, List<Action<IQueueDeclarationBuilder>>> QueueActionListFunc;
		protected Func<IPipeContext, Action<IQueueDeclarationBuilder>> QueueSuffixFunc;
		protected Func<IPipeContext, bool> ActivatedFlagFunc;

		public HostnameQueueSuffixMiddleware(HostnameQueueSuffixOptions options = null)
		{
			QueueActionListFunc = options?.QueueActionListFunc ?? (context => context.GetQueueActions());
			QueueSuffixFunc = options?.QueueSuffixFunc;
			ActivatedFlagFunc = options?.ActivatedFlagFunc ?? (context => context.GetHostnameQueueSuffixFlag());
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
					builder.WithNameSuffix(Environment.MachineName.ToLower());
				};
			}

			protected virtual void AddSuffixAction(List<Action<IQueueDeclarationBuilder>> queueActions, Action<IQueueDeclarationBuilder> suffixAction)
			{
				queueActions.Add(suffixAction);
			}
	}

	public static class HostnameSuffixExtension
	{
		private const string HostnameQueueSuffix = "HostnameQueueSuffix";

		public static IPipeContext UseHostnameQueueSuffix(this IPipeContext context, bool use = true)
		{
			context.Properties.TryAdd("HostnameQueueSuffix", use);
			return context;
		}

		public static bool GetHostnameQueueSuffixFlag(this IPipeContext context)
		{
			return context.Get(HostnameQueueSuffix, false);
		}
	}
}
