using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RawRabbit.Pipe
{
	public interface IPipeContextFactory
	{
		IPipeContext CreateContext(params KeyValuePair<string, object>[] additional);
	}

	public class PipeContextFactory : IPipeContextFactory
	{
		public IPipeContext CreateContext(params KeyValuePair<string, object>[] additional)
		{
			return new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>(additional)
				{
				}
			};
		}
	}
}