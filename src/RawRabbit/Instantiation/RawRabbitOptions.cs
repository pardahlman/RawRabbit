using System;
using RawRabbit.DependecyInjection;

namespace RawRabbit.Instantiation
{
	public class RawRabbitOptions
	{
		public Action<IDependecyRegister> DependencyInjection { get; set; }
		public Action<IClientBuilder> Plugins { get; set; }
	}
}