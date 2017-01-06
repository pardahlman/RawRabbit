using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Instantiation;
using RawRabbit.Operations.Saga.Model;
using RawRabbit.Operations.Saga.Repository;

namespace RawRabbit.Operations.Saga
{
	public static class StateMachinePlugin
	{
		/// <summary>
		/// Registers dependencies for the StateMachine operation.
		/// 
		/// StateMachine models are stored in-memory, but can
		/// be changed by registrating functions for get, add and update.
		/// The execution is only exclusive in the process. If usesd in
		/// a distributed environment, consider changing this by
		/// register an own execute func.
		/// </summary>
		/// <param name="builder">The client builder</param>
		/// <param name="get">Get for model (repo)</param>
		/// <param name="addOrUpdate">Add or update (repo)</param>
		/// <param name="execute">Method to ensure exclusive execution</param>
		/// <returns></returns>
		public static IClientBuilder UseStateMachine(
			this IClientBuilder builder,
			Func<Guid, Task<SagaModel>> get = null,
			Func<SagaModel, Task> addOrUpdate = null,
			Func<Guid, Func<Task>, CancellationToken, Task> execute = null)
		{
			builder.Register(
				pipe => {},
				ioc => ioc
					.AddSingleton<IGlobalLock>(new GlobalLock(execute))
					.AddSingleton<IModelRepository>(new ModelRepository(get, addOrUpdate))
					.AddSingleton<ISagaActivator, SagaActivator>()
				);
			return builder;
		}
	}
}
