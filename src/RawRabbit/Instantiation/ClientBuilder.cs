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
		public Action<IDependecyRegister> ServiceAction { get; set; }

		public ClientBuilder()
		{
			PipeBuilderAction = builder => { };
			ServiceAction = collection => { };
		}

		public void Register(Action<IPipeBuilder> pipe, Action<IDependecyRegister> ioc)
		{
			PipeBuilderAction += pipe;
			ServiceAction += ioc;
		}
	}
}
