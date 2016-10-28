using System;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Stages
{
	public static class PipeContextExtension
	{
		public static Func<object, Task> GetMessageHandler(this IPipeContext context)
		{
			return Get<Func<object, Task>>(context, PipeKey.MessageHandler);
		}

		public static TType Get<TType>(this IPipeContext context, string key, TType fallback = default(TType))
		{
			if (context?.Properties == null)
			{
				return fallback;
			}
			object result;
			if (context.Properties.TryGetValue(key, out result))
			{
				return result is TType ? (TType)result : fallback;
			}
			return fallback;
		}
	}
}
