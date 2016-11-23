using System;

namespace RawRabbit.Context
{
    public class MessageContext : IMessageContext
    {
        public Guid GlobalRequestId { get; set; }
    }
}
