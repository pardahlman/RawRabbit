using System;
using Microsoft.Extensions.DependencyInjection;
using RawRabbit.Configuration.Queue;
using RawRabbit.Context;
using RawRabbit.Extensions.MessageSequence.Core;
using RawRabbit.Extensions.MessageSequence.Core.Abstraction;
using RawRabbit.Extensions.MessageSequence.Repository;

namespace RawRabbit.Extensions.Client
{
	public static class IServiceCollectionExtension
	{
		public static IServiceCollection AddRawRabbitExtensions<TMessageContext>(this IServiceCollection collection) where  TMessageContext : IMessageContext
		{
			collection
				.AddSingleton<IMessageChainDispatcher, MessageChainDispatcher>()
				.AddSingleton<IMessageSequenceRepository, MessageSequenceRepository>()
				.AddSingleton<IMessageChainTopologyUtil, MessageChainTopologyUtil<TMessageContext>>()
				.AddSingleton(c =>
				{
					var chainQueue = QueueConfiguration.Default;
					chainQueue.QueueName = $"rawrabbit_chain_{Guid.NewGuid()}";
					chainQueue.AutoDelete = true;
					chainQueue.Exclusive = true;
					return chainQueue;
				});
			return collection;
		}
	}
}
