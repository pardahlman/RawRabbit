using System;
using RawRabbit.Configuration;
using RawRabbit.Context;
using RawRabbit.Extensions.Client;
using RawRabbit.Extensions.MessageSequence.Configuration;
using RawRabbit.Extensions.MessageSequence.Configuration.Abstraction;
using RawRabbit.Extensions.MessageSequence.Core.Abstraction;
using RawRabbit.Extensions.MessageSequence.Model;
using RawRabbit.Extensions.MessageSequence.Repository;

namespace RawRabbit.Extensions.MessageSequence
{
	public static class MessageSequenceExtension
	{
		public static MessageSequence<TCompleteType> ExecuteSequence<TMessageContext, TCompleteType>(
			this IBusClient<TMessageContext> client,
			Func<IMessageChainPublisher<TMessageContext>, MessageSequence<TCompleteType>> cfg
			) where TMessageContext : IMessageContext
		{
			var extended = (client as ExtendableBusClient<TMessageContext>);
			if (extended == null)
			{
				throw new InvalidOperationException("Extensions is only available for ExtendableBusClient");
			}
			if (cfg == null)
			{
				throw new ArgumentNullException(nameof(cfg));
			}

			var chainTopology = extended.GetService<IMessageChainTopologyUtil>();
			var messageDispather = extended.GetService<IMessageChainDispatcher>();
			var repo = extended.GetService<IMessageSequenceRepository>();
			var mainCfg = extended.GetService<RawRabbitConfiguration>();
			
			var configBuilder = new MessageSequenceBuilder<TMessageContext>(extended, chainTopology, messageDispather, repo, mainCfg);
			return cfg(configBuilder);
		}
	}
}
