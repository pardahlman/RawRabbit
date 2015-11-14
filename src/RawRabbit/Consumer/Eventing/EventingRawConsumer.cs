using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Consumer.Contract;
using RawRabbit.Logging;

namespace RawRabbit.Consumer
{
	class EventingRawConsumer : EventingBasicConsumer, IRawConsumer
	{
		private readonly ILogger _logger = LogManager.GetLogger<EventingRawConsumer>();
		public List<ulong> NackedDeliveryTags { get; private set; } 

		public EventingRawConsumer(IModel channel) : base(channel)
		{
			NackedDeliveryTags = new List<ulong>();
			SetupLogging(this);
		}

		private void SetupLogging(EventingBasicConsumer rawConsumer)
		{
			rawConsumer.Shutdown += (sender, args) =>
			{
				_logger.LogInformation($"Consumer {rawConsumer.ConsumerTag} has been shut down.\n  Reason: {args.Cause}\n  Initiator: {args.Initiator}\n  Reply Text: {args.ReplyText}");
			};
			rawConsumer.ConsumerCancelled +=
				(sender, args) => _logger.LogDebug($"The consumer with tag '{args.ConsumerTag}' has been cancelled.");
			rawConsumer.Unregistered +=
				(sender, args) => _logger.LogDebug($"The consumer with tag '{args.ConsumerTag}' has been unregistered.");
		}

		public Func<object, BasicDeliverEventArgs, Task> OnMessageAsync { get; set; }

		public void Disconnect()
		{
			Model.BasicCancel(ConsumerTag);
		}
	}
}
