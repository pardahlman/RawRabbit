using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RawRabbit.DependencyInjection
{
	public class SimpleDependencyInjection : IDependencyRegister, IDependencyResolver
	{
		private readonly Dictionary<Type, Func<IDependencyResolver, object>> _registrations = new Dictionary<Type, Func<IDependencyResolver, object>>();

		public IDependencyRegister AddTransient<TService, TImplementation>(Func<IDependencyResolver, TImplementation> instanceCreator) where TService : class where TImplementation : class, TService
		{
			if (_registrations.ContainsKey(typeof(TService)))
			{
				_registrations.Remove(typeof(TService));
			}
			_registrations.Add(typeof(TService), instanceCreator);
			return this;
		}

		public IDependencyRegister AddTransient<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			AddTransient<TService, TImplementation>(resolver => GetService(typeof(TImplementation)) as TImplementation);
			return this;
		}

		public IDependencyRegister AddSingleton<TService>(TService instance) where TService : class
		{
			AddTransient<TService, TService>(resolver => instance);
			return this;
		}

		public IDependencyRegister AddSingleton<TService, TImplementation>(Func<IDependencyResolver, TService> instanceCreator) where TImplementation : class, TService where TService : class
		{
			var lazy = new Lazy<TImplementation>(() => (TImplementation)instanceCreator(this));
			AddTransient<TService,TImplementation>(resolver => lazy.Value);
			return this;
		}

		public IDependencyRegister AddSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class
		{
			var lazy = new Lazy<TImplementation>(() => (TImplementation)CreateInstance(typeof(TImplementation), Enumerable.Empty<object>()));
			AddTransient<TService, TImplementation>(resolver => lazy.Value);
			return this;
		}

		public TService GetService<TService>(params object[] additional)
		{
			return (TService)GetService(typeof(TService), additional);
		}

		public object GetService(Type serviceType, params object[] additional)
		{
			Func<IDependencyResolver, object> creator;
			if (_registrations.TryGetValue(serviceType, out creator))
			{
				return creator(this);
			}
			if (!serviceType.GetTypeInfo().IsAbstract)
			{
				return CreateInstance(serviceType, additional);
			}
			throw new InvalidOperationException("No registration for " + serviceType);
		}

		public bool TryGetService(Type serviceType, out object service, params object[] additional)
		{
			Func<IDependencyResolver, object> creator;
			if (_registrations.TryGetValue(serviceType, out creator))
			{
				service = creator(this);
				return true;
			}
			if (!serviceType.GetTypeInfo().IsAbstract)
			{
				service = CreateInstance(serviceType, additional);
				return true;
			}
			service = null;
			return false;
		}

		private object CreateInstance(Type implementationType, IEnumerable<object> additional)
		{
			var additionalTypes = additional.Select(a => a.GetType());
			var ctors = implementationType
				.GetConstructors();
			var ctor = ctors
				.Where(c => c.GetParameters().All(p => p.Attributes.HasFlag(ParameterAttributes.Optional) || additionalTypes.Contains(p.ParameterType) || _registrations.Keys.Contains(p.ParameterType)))
				.OrderByDescending(c => c.GetParameters().Length)
				.FirstOrDefault();
			if (ctor == null)
			{
				throw new Exception($"Unable to find suitable constructor for {implementationType.Name}.");
			}
			var dependencies = ctor
				.GetParameters()
				.Select(parameter =>
				{
					if (additionalTypes.Contains(parameter.ParameterType))
					{
						return additional.First(a => a.GetType() == parameter.ParameterType);
					}
					object service;
					return TryGetService(parameter.ParameterType, out service) ? service : null;
				})
				.ToArray();
			return ctor.Invoke(dependencies);
		}
	}
}
