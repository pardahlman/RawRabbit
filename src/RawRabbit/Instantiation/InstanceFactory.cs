using System;
using RawRabbit.Common;
using RawRabbit.DependecyInjection;
using RawRabbit.Pipe;

namespace RawRabbit.Instantiation
{
	public interface IInstanceFactory
	{
		IBusClient Create();
	}

	public class InstanceFactory : IDisposable, IInstanceFactory
	{
		private readonly IDependecyResolver _resolver;

		public InstanceFactory(IDependecyResolver resolver)
		{
			_resolver = resolver;
		}

		public IBusClient Create()
		{
			return new BusClient(_resolver.GetService<IPipeBuilderFactory>(), _resolver.GetService<IPipeContextFactory>());
		}

		public void Dispose()
		{
			var diposer = _resolver.GetService<IResourceDisposer>();
			diposer?.Dispose();
		}
	}
}
