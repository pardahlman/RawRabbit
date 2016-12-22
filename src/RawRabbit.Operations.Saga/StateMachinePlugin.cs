using System;
using System.Threading.Tasks;
using RawRabbit.Instantiation;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Operations.Saga.Repository;

namespace RawRabbit.Operations.Saga
{
	public static class StateMachinePlugin
	{
		public static IClientBuilder UseStateMachine(
			this IClientBuilder builder,
			Func<Guid, Task<SagaModel>> get = null,
			Func<SagaModel, Task> addOrUpdate = null)
		{
			builder.Register(
				pipe => {},
				ioc => ioc
					.AddSingleton<IGlobalLock, GlobalLock>()
					.AddSingleton<IModelRepository>(new ModelRepository(get, addOrUpdate))
					.AddSingleton<ISagaActivator, SagaActivator>()
				);
			return builder;
		}
	}
}
