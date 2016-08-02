using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Abstraction;

namespace RawRabbit.Extensions.Client
{
	public interface IBusClient<out TMessageContext> : RawRabbit.IBusClient<TMessageContext> where TMessageContext : IMessageContext
	{
		TService GetService<TService>();
	}

	public class ExtendableBusClient<TMessageContext> : BaseBusClient<TMessageContext>, IBusClient<TMessageContext> where TMessageContext : IMessageContext
	{
		private readonly IServiceProvider _serviceProvider;

		public ExtendableBusClient(IServiceProvider serviceProvider)
			: base(
				serviceProvider.GetService<IConfigurationEvaluator>(),
				serviceProvider.GetService< ISubscriber<TMessageContext>>(),
				serviceProvider.GetService<IPublisher>(),
				serviceProvider.GetService<IResponder<TMessageContext>>(),
				serviceProvider.GetService<IRequester>()
			)
		{
			_serviceProvider = serviceProvider;
		}

		public TService GetService<TService>()
		{
			return _serviceProvider.GetService<TService>();
		}
	}
}
