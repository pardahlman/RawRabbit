using System;
using RawRabbit.DependecyInjection;
using RawRabbit.Pipe;

namespace RawRabbit.Instantiation
{
	public interface IClientBuilder
	{
		void Register(Action<IPipeBuilder> pipe, Action<IDependecyRegister> ioc = null);
	}

	public class ClientBuilder : IClientBuilder
	{
		public Action<IPipeBuilder> PipeBuilderAction { get; set; }
		public Action<IDependecyRegister> DependencyInjection { get; set; }

		public ClientBuilder()
		{
			PipeBuilderAction = builder => { };
			DependencyInjection = collection => { };
		}

		public void Register(Action<IPipeBuilder> pipe, Action<IDependecyRegister> ioc)
		{
			PipeBuilderAction += pipe;
			DependencyInjection += ioc;
		}
	}
}
