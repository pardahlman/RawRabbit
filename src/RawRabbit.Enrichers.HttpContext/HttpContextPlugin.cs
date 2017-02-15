using RawRabbit.Enrichers.HttpContext;
using RawRabbit.Instantiation;

namespace RawRabbit
{
	public static class HttpContextPlugin
	{
		public static IClientBuilder UseHttpContext(this IClientBuilder builder)
		{
#if net451
			builder.Register(p => p
				.Use<NetFxHttpContextMiddleware>()
			);
#endif
#if NETSTANDARD1_6
			builder.Register(
				p => p.Use<AspNetCoreHttpContextMiddleware>(),
				p => p.AddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor, Microsoft.AspNetCore.Http.HttpContextAccessor>()
			);
#endif
			return builder;
		}
	}
}
