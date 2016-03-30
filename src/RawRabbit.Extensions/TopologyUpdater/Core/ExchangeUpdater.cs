using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Extensions.TopologyUpdater.Core.Abstraction;

namespace RawRabbit.Extensions.TopologyUpdater.Core
{
	public class ExchangeUpdater : IExchangeUpdater
	{
		private readonly IBindingProvider _bindingProvider;
		private readonly IChannelFactory _channelFactory;
		private const string QueueDestination = "queue";

		public ExchangeUpdater(IBindingProvider bindingProvider, IChannelFactory channelFactory)
		{
			_bindingProvider = bindingProvider;
			_channelFactory = channelFactory;
		}

		public Task UpdateExchangeAsync(ExchangeConfiguration config)
		{
			var channelTask = _channelFactory.GetChannelAsync();
			var bindingsTask = _bindingProvider.GetBindingsAsync(config.ExchangeName);

			return Task
				.WhenAll(channelTask, bindingsTask)
				.ContinueWith(t =>
				{
					var channel = channelTask.Result;
					channel.ExchangeDelete(config.ExchangeName);
					channel.ExchangeDeclare(config.ExchangeName, config.ExchangeType.ToString(), config.Durable, config.AutoDelete,
						config.Arguments);
					foreach (var binding in bindingsTask.Result)
					{
						if (string.Equals(binding.DestinationType, QueueDestination, StringComparison.InvariantCultureIgnoreCase))
						{
							channel.QueueBind(binding.Destination, config.ExchangeName, binding.RoutingKey,
								binding.Arguments as IDictionary<string, object>);
						}
					}
				});
		}

		public Task UpdateExchangesAsync(IEnumerable<ExchangeConfiguration> configs)
		{
			var updateTasks = configs.Select(UpdateExchangeAsync);
			return Task.WhenAll(updateTasks);
		}
	}
}
