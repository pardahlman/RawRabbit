using System.Threading;

#if NET451
using System.Runtime.Remoting.Messaging;
#endif

namespace RawRabbit.Enrichers.GlobalExecutionId.Dependencies
{
	public class GlobalExecutionIdRepository
	{
#if NETSTANDARD1_5 || NETSTANDARD2_0
		private static readonly AsyncLocal<string> GlobalExecutionId = new AsyncLocal<string>();
#elif NET451
		protected const string GlobalExecutionId = "RawRabbit:GlobalExecutionId";
#endif
		
		public static string Get()
		{
#if NETSTANDARD1_5 || NETSTANDARD2_0
			return GlobalExecutionId?.Value;
#elif NET451
			return CallContext.LogicalGetData(GlobalExecutionId) as string;
#endif
		}

		public static void Set(string id)
		{
#if NETSTANDARD1_5 || NETSTANDARD2_0
			GlobalExecutionId.Value = id;
#elif NET451
			CallContext.LogicalSetData(GlobalExecutionId, id);
#endif
		}
	}
}
