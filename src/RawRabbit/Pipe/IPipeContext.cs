using System.Collections.Generic;

namespace RawRabbit.Pipe
{
	public interface IPipeContext
	{
		IDictionary<string, object> Properties { get; }
	}

	public class PipeContext : IPipeContext
	{
		public IDictionary<string, object> Properties { get; set;  }
	}
}