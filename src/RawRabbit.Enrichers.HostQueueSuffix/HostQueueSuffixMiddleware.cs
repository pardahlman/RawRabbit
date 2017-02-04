using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.HostQueueSuffix
{
	public class HostQueueSuffixMiddleware : StagedMiddleware
	{
		protected Func<IPipeContext, List<Action<IQueueDeclarationBuilder>>> QueueActionListFunc;
		protected Func<IPipeContext, Action<IQueueDeclarationBuilder>> QueueSuffixFunc;
		protected Func<IPipeContext, bool> ActivatedFlagFunc;
		public override string StageMarker => Pipe.StageMarker.Initialized;

		public HostQueueSuffixMiddleware(HostQueueSuffixOptions options = null)
		{
			QueueActionListFunc = options?.QueueActionListFunc ?? (context => context.GetQueueActions());
			QueueSuffixFunc = options?.QueueSuffixFunc ?? (context => context.GetQueueSuffixFunc());
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
			return QueueSuffixFunc.Invoke(context) ?? (builder =>
				{
					builder.WithNameSuffix(Environment.MachineName.ToLower());
				});
			}

			protected virtual void AddSuffixAction(List<Action<IQueueDeclarationBuilder>> queueActions, Action<IQueueDeclarationBuilder> suffixAction)
			{
				queueActions.Add(suffixAction);
			}
	}
}
