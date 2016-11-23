using System;
using System.Collections.Generic;

namespace RawRabbit.Extensions.BulkGet.Configuration
{
    public class MessageConfiguration
    {
        public Type MessageType { get; set; }
        public List<string> QueueNames { get; set; }
        public bool GetAll { get; set; }
        public int BatchSize { get; set; }
        public bool NoAck { get; set; }

        public MessageConfiguration()
        {
            QueueNames = new List<string>();
        }
    }
}