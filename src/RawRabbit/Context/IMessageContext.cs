using System;

namespace RawRabbit.Context
{
    public interface IMessageContext
    {
        Guid GlobalRequestId { get; set; }
    }
}