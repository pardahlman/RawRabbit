using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RawRabbit.Core.Configuration.Exchange;
using RawRabbit.Core.Configuration.Queue;

namespace RawRabbit.Core.Configuration.Operation
{
	public abstract class ConfigurationBuilderBase
	{
		protected QueueConfigurationBuilder _replyQueue;
		protected ExchangeConfigurationBuilder _exchange;
		protected string RoutingKey;

		protected ConfigurationBuilderBase()
		{
			_replyQueue = new QueueConfigurationBuilder();
			_exchange = new ExchangeConfigurationBuilder();
		}

	}
}
