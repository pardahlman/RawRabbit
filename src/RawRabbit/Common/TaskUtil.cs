using System;
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

		public static Task FromException(Exception exception)
		{
			return FromException<object>(exception);
		}

		public static Task<T> FromException<T>(Exception exception)
		{
			var tsc = new TaskCompletionSource<T>();
			tsc.TrySetException(exception);
			return tsc.Task;
		}
	}
}
