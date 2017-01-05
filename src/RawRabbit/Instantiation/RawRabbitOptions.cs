using System;
using RawRabbit.Configuration;
using RawRabbit.DependecyInjection;

namespace RawRabbit.Instantiation
{
	public class RawRabbitOptions
	{
		public RawRabbitConfiguration ClientConfiguration { get; set; }
		public Action<IDependecyRegister> DependencyInjection { get; set; }
		public Action<IClientBuilder> Plugins { get; set; }
	}
}