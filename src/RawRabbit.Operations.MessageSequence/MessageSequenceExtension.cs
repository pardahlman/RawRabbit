using System;
using RawRabbit.Context;
using RawRabbit.Operations.MessageSequence.Configuration.Abstraction;
using RawRabbit.Operations.MessageSequence.Model;
using RawRabbit.Operations.Saga;
using RawRabbit.Operations.Saga.Middleware;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.MessageSequence
{
	public static class MessageSequenceExtension
	{
		private const string MessageSequence = "MessageSequence";

		public static MessageSequence<TCompleteType> ExecuteSequence<TMessageContext, TCompleteType>(
			this IBusClient client,
			Func<IMessageChainPublisher<TMessageContext>, MessageSequence<TCompleteType>> cfg
		) where TMessageContext : IMessageContext, new()
		{
			var sequenceSaga = new StateMachine.MessageSequence<TMessageContext>(client);
			return cfg(sequenceSaga);
			//return client
			//	.InvokeAsync(pipe => pipe
			//		.Use((context, func) =>
			//		{
			//			var sequenceSaga = new StateMachine.MessageSequence<TMessageContext>(client);
			//			context.Properties.Add(SagaKey.Saga, sequenceSaga);
			//			return func();
			//		})
			//		.Use<PersistSagaMiddleware>()
			//		.Use((context, func) =>
			//		{
			//			var sequenceSaga = context.Get<StateMachine.MessageSequence<TMessageContext>>(SagaKey.Saga);
			//			var result = cfg(sequenceSaga);
			//			context.Properties.Add(MessageSequence, result);
			//			return func();
			//		}))
			//	.ContinueWith(tContext => tContext.Result.Get<Model.MessageSequence<TCompleteType>>(MessageSequence))
			//	.GetAwaiter()
			//	.GetResult();
		}
	}
}
