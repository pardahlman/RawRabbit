using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Configuration.Respond;
using RawRabbit.Consumer.Contract;

namespace RawRabbit.Consumer.Queueing
{
	public class QueueingBaiscConsumerFactory : IConsumerFactory
	{
		private readonly IChannelFactory _channelFactory;

		public QueueingBaiscConsumerFactory(IChannelFactory channelFactory)
		{
			_channelFactory = channelFactory;
		}

		public IRawConsumer CreateConsumer(IConsumerConfiguration cfg)
		{
			var channel = _channelFactory.GetChannel();
			ConfigureQos(channel, cfg.PrefetchCount);
			var basicConsumer = new QueueingBasicConsumer(channel);
			channel.BasicConsume(cfg.Queue.QueueName, cfg.NoAck, basicConsumer);
			
			var rawConsumer =new QueueingRawConsumer(channel);
			
			//TODO: Add exception handling etc.

			Task
				.Run(() => basicConsumer.Queue.Dequeue())
				.ContinueWith(argsTask => rawConsumer.OnMessageAsync(this, argsTask.Result));

			return rawConsumer;
		}

		protected void ConfigureQos(IModel channel, ushort prefetchCount)
		{
			channel.BasicQos(
				prefetchSize: 0,
				prefetchCount: prefetchCount,
				global: false
			);
		}
	}
}
