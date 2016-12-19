using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RawRabbit.Operations.Saga.Middleware;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Operations.Saga.Trigger;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Saga
{
	public static class PipeContextExtensions
	{
		public static Model.Saga GetSaga(this IPipeContext context)
		{
			return context.Get<Model.Saga>(SagaKey.Saga);
		}

		public static Guid GetSagaId(this IPipeContext context)
		{
			return context.Get<Guid>(SagaKey.SagaId);
		}

		public static List<SagaSubscriberOptions> GetSagaSubscriberOptions(this IPipeContext context)
		{
			return context.Get<List<SagaSubscriberOptions>>(SagaKey.SagaSubscriberOptions);
		}

		public static Action<IPipeContext> GetContextAction(this IPipeContext context)
		{
			return context.Get<Action<IPipeContext>>(SagaKey.ContextAction);
		}

		public static Action<IPipeBuilder> GetPipeBuilderAction(this IPipeContext context)
		{
			return context.Get<Action<IPipeBuilder>>(SagaKey.PipeBuilderAction);
		}

		public static Func<object, Guid> GetIdCorrelationFunc(this IPipeContext context)
		{
			return context.Get<Func<object, Guid>>(SagaKey.CorrelationFunc);
		}
	}
}
