using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Get;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware
{
	public class ConventionNamingOptions
	{
		public Func<IPipeContext, GetConfiguration> GetConfigFunc { get; set; }
		public Func<IPipeContext, Type> MessageTypeFunc { get; set; }
	}

	public class ConventionNamingMiddleware : Pipe.Middleware.Middleware
	{
		private readonly INamingConventions _conventions;
		protected Func<IPipeContext, GetConfiguration> GetConfigFunc;
		protected Func<IPipeContext, Type> MessageTypeFunc;

		public ConventionNamingMiddleware(INamingConventions conventions, ConventionNamingOptions options = null)
		{
			_conventions = conventions;
			GetConfigFunc = options?.GetConfigFunc ?? (context => context.GetGetConfiguration());
			MessageTypeFunc = options?.MessageTypeFunc ?? (context => context.GetMessageType());
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var config = GetGetConfiguration(context);
			if (!string.IsNullOrWhiteSpace(config.QueueName))
			{
				return Next.InvokeAsync(context);
			}

			var messageType = GetMessageType(context);
			config.QueueName = _conventions.QueueNamingConvention(messageType);
			return Next.InvokeAsync(context);
		}

		protected virtual GetConfiguration GetGetConfiguration(IPipeContext context)
		{
			return GetConfigFunc(context);
		}

		protected virtual Type GetMessageType(IPipeContext context)
		{
			return MessageTypeFunc(context);
		}
	}
}
