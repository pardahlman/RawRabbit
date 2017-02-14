using RawRabbit.Common;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.HttpContext.Properties
{
	public static class HttpContextExtensions
	{
		public const string HttpContext = "HttpContext";

		public static IPipeContext UseHttpContext(this IPipeContext pipeContext)
		{
			return pipeContext;
		}
	}
}
