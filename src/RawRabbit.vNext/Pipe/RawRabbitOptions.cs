using System;
using Microsoft.Extensions.DependencyInjection;

namespace RawRabbit.vNext.Pipe
{
	public class RawRabbitOptions : Instantiation.RawRabbitOptions
	{
		public new Action<IServiceCollection> DependencyInjection { get; set; }
	}
}
