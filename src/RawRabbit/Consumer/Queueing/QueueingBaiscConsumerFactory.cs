using System.Collections.Concurrent;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Channel;
using RawRabbit.Channel.Abstraction;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Abstraction;

namespace RawRabbit.Consumer.Queueing
{
	public class QueueingBaiscConsumerFactory : IConsumerFactory
	{
		private readonly ConcurrentBag<IRawConsumer> _consumers;
		 
		public QueueingBaiscConsumerFactory(IChannelFactory channelFactory)
		{
			_consumers = new ConcurrentBag<IRawConsumer>();
		}
	
		public IRawConsumer CreateConsumer(IConsumerConfiguration cfg, IModel channel)
		{
			ConfigureQos(channel, cfg.PrefetchCount);
			var consumer = new QueueingRawConsumer(channel);
			_consumers.Add(consumer);

			//TODO: Add exception handling etc.

			Task
				.Run(() => consumer.Queue.Dequeue())
				.ContinueWith(argsTask => consumer.OnMessageAsync(this, argsTask.Result));

			return consumer;
		}

		protected void ConfigureQos(IModel channel, ushort prefetchCount)
		{
			channel.BasicQos(
				prefetchSize: 0,
				prefetchCount: prefetchCount,
				global: false
			);
		}

		public void Dispose()
		{
			foreach (var consumer in _consumers)
			{
				consumer?.Disconnect();
			}
		}
	}
}
