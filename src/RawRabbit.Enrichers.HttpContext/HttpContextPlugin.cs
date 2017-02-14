using RawRabbit.Enrichers.HttpContext;
using RawRabbit.Instantiation;

namespace RawRabbit
{
	public static class HttpContextPlugin
	{
		public static IClientBuilder UseHttpContext(this IClientBuilder builder)
		{
			builder.Register(p => p
				.Use<NetFxHttpContextMiddleware>()
				.Use<AspNetCoreHttpContextMiddleware>()
			);
			return builder;
		}
	}
}
