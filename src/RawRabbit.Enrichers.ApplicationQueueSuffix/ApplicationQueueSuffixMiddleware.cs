using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.ApplicationQueueSuffix
{
	public class ApplicationQueueSuffixMiddleware : StagedMiddleware
	{
		protected INamingConventions Conventsions;
		protected Func<IPipeContext, List<Action<IQueueDeclarationBuilder>>> QueueActionListFunc;
		protected Func<IPipeContext, Action<IQueueDeclarationBuilder>> QueueSuffixFunc;
		protected Func<IPipeContext, bool> ActivatedFlagFunc;
		public override string StageMarker => Pipe.StageMarker.Initialized;

		public ApplicationQueueSuffixMiddleware(INamingConventions conventsions, ApplicationQueueSuffixOptions options = null)
		{
			Conventsions = conventsions;
			QueueActionListFunc = options?.QueueActionListFunc ?? (context => context.GetQueueActions());
			QueueSuffixFunc = options?.QueueSuffixFunc;
			ActivatedFlagFunc = options?.ActivatedFlagFunc ?? (context => context.GetApplicationSuffixFlag());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
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
				var msgType = context.GetMessageType();
				builder.WithNameSuffix(Conventsions.SubscriberQueueSuffix(msgType));
			};
		}

		protected virtual void AddSuffixAction(List<Action<IQueueDeclarationBuilder>> queueActions, Action<IQueueDeclarationBuilder> suffixAction)
		{
			queueActions.Add(suffixAction);
		}

	}
}
