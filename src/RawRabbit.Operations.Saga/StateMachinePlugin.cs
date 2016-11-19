using RawRabbit.Instantiation;
using RawRabbit.Operations.Saga.Middleware;
using RawRabbit.Operations.Saga.Repository;

namespace RawRabbit.Operations.Saga
{
	public static class StateMachinePlugin
	{
		public static IClientBuilder UseStateMachine(this IClientBuilder builder)
		{
			builder.Register(
				pipe => {},
				ioc => ioc.AddSingleton<ISagaRepository, InMemoryRepository>());
			return builder;
		}
	}
}
