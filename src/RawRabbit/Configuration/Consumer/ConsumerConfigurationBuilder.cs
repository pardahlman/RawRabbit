using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Exchange;
using RawRabbit.Configuration.Queue;

namespace RawRabbit.Configuration.Consumer
{
	public class ConsumerConfigurationBuilder :  IConsumerConfigurationBuilder
	{
		public ConsumerConfiguration Config { get; }

		public ConsumerConfigurationBuilder(ConsumerConfiguration initial)
		{
			Config = initial;
		}

		public IConsumerConfigurationBuilder OnDeclaredExchange(Action<IExchangeDeclarationBuilder> exchange)
		{
			var builder = new ExchangeDeclarationBuilder(Config.Exchange);
			exchange(builder);
			Config.Exchange = builder.Declaration;
			Config.Consume.ExchangeName = builder.Declaration.ExchangeName;
			return this;
		}

		public IConsumerConfigurationBuilder FromDeclaredQueue(Action<IQueueDeclarationBuilder> queue)
		{
			var builder = new QueueDeclarationBuilder(Config.Queue);
			queue(builder);
			Config.Queue = builder.Configuration;
			Config.Consume.QueueName = builder.Configuration.Name;
			return this;
		}

		public IConsumerConfigurationBuilder Consume(Action<IConsumeConfigurationBuilder> consume)
		{
			var builder = new ConsumeConfigurationBuilder(Config.Consume);
			consume(builder);
			Config.Consume = builder.Config;
			if (builder.ExistingExchange)
			{
				Config.Exchange = null;
			}
			if (builder.ExistingQueue)
			{
				Config.Queue = null;
			}
			return this;
		}
	}
}