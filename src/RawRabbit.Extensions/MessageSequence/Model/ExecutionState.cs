using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.MessageSequence.Model
{
    public class ExecutionState
    {
        public List<ExecutionResult> Skipped { get; set; }
        public List<ExecutionResult> Completed { get; set; }
        public Guid GlobalRequestId { get; set; }
        public bool Aborted { get; set; }
        public List<Task> HandlerTasks { get; set; }

        public ExecutionState()
        {
            Skipped = new List<ExecutionResult>();
            Completed = new List<ExecutionResult>();
            HandlerTasks = new List<Task>();
        }
    }
}