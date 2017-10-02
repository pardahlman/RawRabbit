using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using RawRabbit.Serialization;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicPropertiesOptions
	{
		public Action<IPipeContext, IBasicProperties> PropertyModier { get; set; }
		public Func<IPipeContext, IBasicProperties> GetOrCreatePropsFunc { get; set; }
		public Action<IPipeContext, IBasicProperties> PostCreateAction { get; set; }
	}

	public class BasicPropertiesMiddleware : Middleware
	{
		protected ISerializer Serializer;
		protected Func<IPipeContext, IBasicProperties> GetOrCreatePropsFunc;
		protected Action<IPipeContext, IBasicProperties> PropertyModifier;
		protected Action<IPipeContext, IBasicProperties> PostCreateAction;

		public BasicPropertiesMiddleware(ISerializer serializer, BasicPropertiesOptions options = null)
		{
			Serializer = serializer;
			PropertyModifier = options?.PropertyModier ?? ((ctx, props) => ctx.Get<Action<IBasicProperties>>(PipeKey.BasicPropertyModifier)?.Invoke(props));
			PostCreateAction = options?.PostCreateAction;
			GetOrCreatePropsFunc = options?.GetOrCreatePropsFunc ?? (ctx => ctx.GetBasicProperties() ?? new BasicProperties
			{
				MessageId = Guid.NewGuid().ToString(),
				Headers = new Dictionary<string, object>(),
				Persistent = ctx.GetClientConfiguration().PersistentDeliveryMode,
				ContentType = Serializer.ContentType
			});
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var props = GetOrCreateBasicProperties(context);
			ModifyBasicProperties(context, props);
			InvokePostCreateAction(context, props);
			context.Properties.TryAdd(PipeKey.BasicProperties, props);
			await Next.InvokeAsync(context, token);
		}

		protected virtual void ModifyBasicProperties(IPipeContext context, IBasicProperties props)
		{
			PropertyModifier?.Invoke(context, props);
		}

		protected virtual void InvokePostCreateAction(IPipeContext context, IBasicProperties props)
		{
			PostCreateAction?.Invoke(context, props);
		}

		protected virtual IBasicProperties GetOrCreateBasicProperties(IPipeContext context)
		{
			return GetOrCreatePropsFunc(context);
		}
	}
}
