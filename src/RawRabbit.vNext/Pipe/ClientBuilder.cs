using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Pipe;

namespace RawRabbit.vNext.Pipe
{
	public interface IClientBuilder
	{
		void Register(Action<IPipeBuilder> pipe, Action<IServiceCollection> ioc = null);
	}

	public class ClientBuilder : IClientBuilder
	{
		public Action<IPipeBuilder> PipeBuilderAction { get; set; }
		public Action<IServiceCollection> ServiceAction { get; set; }

		public ClientBuilder()
		{
			PipeBuilderAction = builder => { };
			ServiceAction = collection => { };
		}

		public void Register(Action<IPipeBuilder> pipe, Action<IServiceCollection> ioc)
		{
			PipeBuilderAction += pipe;
			ServiceAction += ioc;
		}
	}
}
