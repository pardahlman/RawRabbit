using System.Collections.Concurrent;
using System.Collections.Generic;
using RawRabbit.Configuration;

namespace RawRabbit.Pipe
{
	public interface IPipeContextFactory
	{
		IPipeContext CreateContext(params KeyValuePair<string, object>[] additional);
	}

	public class PipeContextFactory : IPipeContextFactory
	{
		private readonly RawRabbitConfiguration _config;

		public PipeContextFactory(RawRabbitConfiguration config)
		{
			_config = config;
		}

		public IPipeContext CreateContext(params KeyValuePair<string, object>[] additional)
		{
			return new PipeContext
			{
				Properties = new ConcurrentDictionary<string, object>(additional)
				{
					[PipeKey.ClientConfiguration] = _config
				}
			};
		}
	}
}