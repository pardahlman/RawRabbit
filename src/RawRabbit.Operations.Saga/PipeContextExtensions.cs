using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga
{
	public static class PipeContextExtensions
	{
		public static Model.Saga GetSaga(this IPipeContext context)
		{
			return context.Get<Model.Saga>(SagaKey.Saga);
		}

		public static Func<Model.Saga, Task> GetSagaTriggerFunc(this IPipeContext context)
		{
			return context.Get<Func<Model.Saga, Task>>(SagaKey.TriggerFunc);
		}

		public static List<TriggerInvoker> GetTriggerInvokers(this IPipeContext context)
		{
			return context.Get< List<TriggerInvoker>>(SagaKey.TriggerInvokers);
		}

		public static TriggerInvoker GetTriggerInvoker(this IPipeContext context)
		{
			return context.Get<TriggerInvoker>(SagaKey.TriggerInvoker);
		}
	}
}
