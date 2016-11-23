using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RawRabbit.Consumer.Abstraction;
using RawRabbit.Logging;

namespace RawRabbit.Consumer.Eventing
{
    public class EventingRawConsumer : EventingBasicConsumer, IRawConsumer
    {
        private readonly ILogger _logger = LogManager.GetLogger<EventingRawConsumer>();
        public List<ulong> AcknowledgedTags { get; }

        public EventingRawConsumer(IModel channel) : base(channel)
        {
            AcknowledgedTags = new List<ulong>();
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
            if (string.IsNullOrEmpty(ConsumerTag))
            {
                // broker has not given a tag yet.
                return;
            }
            try
            {
                Model.BasicCancel(ConsumerTag);
            }
            catch (AlreadyClosedException)
            {
                // Perfect, someone allready closed this.
            }
        }
    }
}
