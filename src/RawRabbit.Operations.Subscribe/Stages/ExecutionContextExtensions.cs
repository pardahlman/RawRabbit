using System;
using System.Threading.Tasks;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Subscribe.Stages
{
	public static class PipeContextExtension
	{
		public static Func<object, Task> GetSubscriptionMessageHandler(this IPipeContext context)
		{
			return context.Get<Func<object, Task>>(PipeKey.MessageHandler);
		}
		
	}
}
