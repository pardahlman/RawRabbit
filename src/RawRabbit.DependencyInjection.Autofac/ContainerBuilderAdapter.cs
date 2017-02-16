using System;
using Autofac;
using RawRabbit.DependecyInjection;

namespace RawRabbit.DependencyInjection.Autofac
{
	public class ContainerBuilderAdapter : IDependecyRegister
	{
		private readonly ContainerBuilder _builder;

		public ContainerBuilderAdapter(ContainerBuilder builder)
		{
			_builder = builder;
		}

		public IDependecyRegister AddTransient<TService, TImplementation>(Func<IDependecyResolver, TImplementation> instanceCreator) where TService : class where TImplementation : class, TService
		{
			_builder
				.Register<TImplementation>(context => instanceCreator(new ComponentContextAdapter(context)))
				.As<TService>()
				.InstancePerDependency();
			return this;
		}

		public IDependecyRegister AddTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
		{
			_builder
				.RegisterType<TImplementation>()
				.As<TService>()
				.InstancePerDependency();
			return this;
		}

		public IDependecyRegister AddSingleton<TService>(TService instance) where TService : class
		{
			_builder
				.Register<TService>(context => instance)
				.As<TService>()
				.SingleInstance();
			return this;
		}

		public IDependecyRegister AddSingleton<TService, TImplementation>(Func<IDependecyResolver, TService> instanceCreator) where TService : class where TImplementation : class, TService
		{
			_builder
				.Register<TService>(context => instanceCreator(new ComponentContextAdapter(context)))
				.As<TService>()
				.SingleInstance();
			return this;
		}

		public IDependecyRegister AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
		{
			_builder
				.RegisterType<TImplementation>()
				.As<TService>()
				.SingleInstance();
			return this;
		}
	}
}
