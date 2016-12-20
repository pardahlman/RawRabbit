using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.BasicPublish;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicPublishConfigurationOptions
	{
		public Func<IPipeContext, Action<IBasicPublishConfigurationBuilder>> ConfigurationActionFunc { get; set; }
		public Action<IPipeContext, BasicPublishConfiguration> PostInvokeAction { get; set; }
	}

	public class BasicPublishConfigurationMiddleware : Middleware
	{
		private readonly IBasicPublishConfigurationFactory _factory;
		protected Func<IPipeContext, Action<IBasicPublishConfigurationBuilder>> ConfigurationActionFunc;
		protected Action<IPipeContext, BasicPublishConfiguration> PostInvokeAction;

		public BasicPublishConfigurationMiddleware(IBasicPublishConfigurationFactory factory, BasicPublishConfigurationOptions options = null)
		{
			_factory = factory;
			ConfigurationActionFunc = options?.ConfigurationActionFunc ?? (context => context.Get<Action<IBasicPublishConfigurationBuilder>>(PipeKey.ConfigurationAction));
			PostInvokeAction = options?.PostInvokeAction;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var config = GetInitialConfig(context);
			var configAction = GetConfigurationAction(context);
			if (configAction != null)
			{
				var builder = new BasicPublishConfigurationBuilder(config);
				configAction.Invoke(builder);
				config = builder.Configuration;
			}
			PostInvokeAction?.Invoke(context, config);
			context.Properties.TryAdd(PipeKey.BasicPublishConfiguration, config);
			return Next.InvokeAsync(context, token);
		}

		protected virtual BasicPublishConfiguration GetInitialConfig(IPipeContext context)
		{
			var message = context.GetMessage();
			return message != null
					? _factory.Create(message)
					: _factory.Create();
		}

		protected virtual Action<IBasicPublishConfigurationBuilder> GetConfigurationAction(IPipeContext context)
		{
			return ConfigurationActionFunc.Invoke(context);
		}
	}
}