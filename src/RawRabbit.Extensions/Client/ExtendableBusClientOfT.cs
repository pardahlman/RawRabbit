using System;
using System.Runtime.Remoting.Services;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Contracts;

namespace RawRabbit.Extensions.Client
{
	public class ExtendableBusClient<TMessageContext> : BaseBusClient<TMessageContext> where TMessageContext : IMessageContext
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
