using System.Collections.Generic;
using System.Threading.Tasks;

namespace RawRabbit.Extensions.MessageSequence.Model
{
	public class MessageSequence<TMessageType>
	{
		public Task<TMessageType> Task { get; set; }
		public bool Aborted { get; set; }
		public List<ExecutionResult> Completed { get; set; }
		public List<ExecutionResult> Skipped { get; set; }
	}
}