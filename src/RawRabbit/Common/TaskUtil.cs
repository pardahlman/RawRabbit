using System.Threading.Tasks;

namespace RawRabbit.Common
{
	public class TaskUtil
	{
		public static Task<T> FromCancelled<T>()
		{
			var tsc = new TaskCompletionSource<T>();
			tsc.TrySetCanceled();
			return tsc.Task;
		}

		public static Task FromCancelled()
		{
			return FromCancelled<object>();
		}
	}
}
