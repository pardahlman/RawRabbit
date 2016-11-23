using System;
using System.Collections.Generic;

namespace RawRabbit.Extensions.BulkGet.Configuration
{
    public interface IBulkGetConfigurationBuilder
    {
        IBulkGetConfigurationBuilder ForMessage<TMessage>(Action<IMessageConfigurationBuilder> msg) where TMessage : new();
    }

    public class BulkGetConfigurationBuilder : IBulkGetConfigurationBuilder
    {
        public List<MessageConfiguration> Configurations { get; set; }

        public BulkGetConfigurationBuilder()
        {
            Configurations = new List<MessageConfiguration>();
        }

        public IBulkGetConfigurationBuilder ForMessage<TMessage>(Action<IMessageConfigurationBuilder> msg) where TMessage : new()
        {
            var builder = new MessageConfigurationBuilder<TMessage>();
            msg(builder);
            Configurations.Add(builder.Configuration);
            return this;
        }
    }
}
