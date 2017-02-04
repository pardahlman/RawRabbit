using System;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public static class HostSuffixExtension
	{
		private const string HostnameQueueSuffix = "HostnameQueueSuffix";
		private const string HostQueueSuffixAction = "HostQueueSuffixAction";


		public static IPipeContext UseHostnameQueueSuffix(this IPipeContext context, bool activated)
		{
			context.Properties.TryAdd(HostnameQueueSuffix, activated);
			return context;
		}

		public static IPipeContext UseHostnameQueueSuffix(this IPipeContext context, Action<IQueueDeclarationBuilder> queueSuffix = null)
		{
			context.Properties.TryAdd(HostnameQueueSuffix, true);
			if (queueSuffix != null)
			{
				context.Properties.AddOrReplace(HostQueueSuffixAction, queueSuffix);
			}
			return context;
		}

		public static bool GetHostnameQueueSuffixFlag(this IPipeContext context)
		{
			return context.Get(HostnameQueueSuffix, true);
		}

		public static Action<IQueueDeclarationBuilder> GetQueueSuffixFunc(this IPipeContext context)
		{
			return context.Get<Action<IQueueDeclarationBuilder>>(HostQueueSuffixAction);
		}
	}
}