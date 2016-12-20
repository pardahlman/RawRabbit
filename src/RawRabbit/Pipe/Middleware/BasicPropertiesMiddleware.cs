using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicPropertiesOptions
	{
		public Action<IPipeContext, IBasicProperties> PropertyModier { get; set; }
		public Func<IPipeContext, IBasicProperties> CreatePropsFunc { get; set; }
		public Action<IPipeContext, IBasicProperties> PostCreateAction { get; set; }
	}

	public class BasicPropertiesMiddleware : Middleware
	{
		protected Func<IPipeContext, IBasicProperties> CreatePropsFunc;
		protected Action<IPipeContext, IBasicProperties> PropertyModifier;
		protected Action<IPipeContext, IBasicProperties> PostCreateAction;

		public BasicPropertiesMiddleware(BasicPropertiesOptions options = null)
		{
			PropertyModifier = options?.PropertyModier ?? ((ctx, props) => ctx.Get<Action<IBasicProperties>>(PipeKey.BasicPropertyModifier)?.Invoke(props));
			PostCreateAction = options?.PostCreateAction;
			CreatePropsFunc = options?.CreatePropsFunc ?? (ctx => new BasicProperties
			{
				MessageId = Guid.NewGuid().ToString(),
				Headers = new Dictionary<string, object>(),
				Persistent = ctx.GetClientConfiguration().PersistentDeliveryMode
			});
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var props = CreateBasicProperties(context);
			ModifyBasicProperties(context, props);
			InvokePostCreateAction(context, props);
			context.Properties.TryAdd(PipeKey.BasicProperties, props);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void ModifyBasicProperties(IPipeContext context, IBasicProperties props)
		{
			PropertyModifier?.Invoke(context, props);
		}

		protected virtual void InvokePostCreateAction(IPipeContext context, IBasicProperties props)
		{
			PostCreateAction?.Invoke(context, props);
		}

		protected virtual IBasicProperties CreateBasicProperties(IPipeContext context)
		{
			return CreatePropsFunc(context);
		}
	}
}
