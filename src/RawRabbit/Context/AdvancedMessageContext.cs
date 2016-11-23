using System;

namespace RawRabbit.Context
{
    public class AdvancedMessageContext : MessageContext, IAdvancedMessageContext
    {
        public Action Nack { get; set; }
        public Action<TimeSpan> RetryLater { get; set; }
        public RetryInformation RetryInfo { get; set; }
    }

    public class RetryInformation
    {
        public long NumberOfRetries { get; set; }
        public DateTime OriginalSent { get; set; }
    }
}
