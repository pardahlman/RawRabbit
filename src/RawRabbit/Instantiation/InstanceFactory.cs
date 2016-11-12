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
			var pipeBuliderFactory = new PipeBuilderFactory(() => new PipeBuilder(_resolver));
			return new BusClient(pipeBuliderFactory, _resolver.GetService<IPipeContextFactory>());
		}

		public void Dispose()
		{
			var diposer = _resolver.GetService<IResourceDisposer>();
			diposer?.Dispose();
		}
	}
}
