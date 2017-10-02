using System;

namespace RawRabbit.Pipe.Middleware
{
	public class MiddlewareInfo
	{
		public Type Type { get; set; }
		public object[] ConstructorArgs { get; set; }
	}
}