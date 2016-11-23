using System;

namespace RawRabbit.Extensions.MessageSequence.Model
{
    public class ExecutionResult
    {
        public Guid StepId { get; set; }
        public DateTime Time { get; set; }
        public Type Type { get; set; }
    }
}