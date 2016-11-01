using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.DependecyInjection;

namespace RawRabbit.vNext.Pipe
{
	public class RawRabbitOptions : Instantiation.RawRabbitOptions
	{
		public new Action<IServiceCollection> DependencyInjection { get; set; }
		public Action<IConfigurationBuilder> Configuration { get; set; }
	}
}
