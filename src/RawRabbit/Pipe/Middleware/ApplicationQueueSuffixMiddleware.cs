using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Pipe.Middleware
{
	public class ApplicationQueueSuffix
	{
		public Func<IPipeContext, List<Action<IQueueDeclarationBuilder>>> QueueActionListFunc { get; set; }
		public Func<IPipeContext, Action<IQueueDeclarationBuilder>> QueueSuffixFunc { get; set; }
		public Func<IPipeContext, bool> ActivatedFlagFunc { get; set; }
	}

	public class ApplicationQueueSuffixMiddleware : Middleware
	{
		protected INamingConventions Conventsions;
		protected Func<IPipeContext, List<Action<IQueueDeclarationBuilder>>> QueueActionListFunc;
		protected Func<IPipeContext, Action<IQueueDeclarationBuilder>> QueueSuffixFunc;
		protected Func<IPipeContext, bool> ActivatedFlagFunc;

		public ApplicationQueueSuffixMiddleware(INamingConventions conventsions, ApplicationQueueSuffix options = null)
		{
			Conventsions = conventsions;
			QueueActionListFunc = options?.QueueActionListFunc ?? (context => context.GetQueueActions());
			QueueSuffixFunc = options?.QueueSuffixFunc;
			ActivatedFlagFunc = options?.ActivatedFlagFunc ?? (context => true);
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

	public static class ApplicationSuffixExtension
	{
		private const string ApplicationQueueSuffix = "ApplicationQueueSuffix";

		public static IPipeContext UseApplicationQueueSuffix(this IPipeContext context, bool use = true)
		{
			context.Properties.TryAdd("ApplicationQueueSuffix", use);
			return context;
		}

		public static bool GetApplicationSuffixFlag(this IPipeContext context)
		{
			return context.Get(ApplicationQueueSuffix, false);
		}
	}
}
