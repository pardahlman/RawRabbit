using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Logging;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class MandatoryCallbackOptions
	{
		public Func<IPipeContext, EventHandler<BasicReturnEventArgs>> CallbackFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Action<IPipeContext, EventHandler<BasicReturnEventArgs>> PostInvokeAction { get; set; }
	}

	public class MandatoryCallbackMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, EventHandler<BasicReturnEventArgs>> CallbackFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Action<IPipeContext, EventHandler<BasicReturnEventArgs>> PostInvoke;
		private readonly ILogger _logger = LogManager.GetLogger<MandatoryCallbackMiddleware>();

		public MandatoryCallbackMiddleware(MandatoryCallbackOptions options = null)
		{
			CallbackFunc = options?.CallbackFunc ?? (context => context.GetReturnedMessageCallback());
			ChannelFunc = options?.ChannelFunc?? (context => context.GetTransientChannel());
			PostInvoke = options?.PostInvokeAction;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var callback = GetCallback(context);
			if (callback == null)
			{
				_logger.LogDebug("No Mandatory Callback registered.");
				await Next.InvokeAsync(context, token);
				return;
			}

			var channel = GetChannel(context);
			if (channel == null)
			{
				_logger.LogWarning("Channel not found in Pipe Context. Mandatory Callback not registered.");
				await Next.InvokeAsync(context, token);
				return;
			}

			_logger.LogDebug($"Register Mandatory Callback on channel '{channel.ChannelNumber}'");
			channel.BasicReturn += callback;
			PostInvoke?.Invoke(context, callback);

			await Next.InvokeAsync(context, token);
			_logger.LogDebug($"Removing Mandatory Callback on channel '{channel.ChannelNumber}'");
			channel.BasicReturn -= callback;
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}

		protected virtual EventHandler<BasicReturnEventArgs> GetCallback(IPipeContext context)
		{
			return CallbackFunc(context);
		}
	}
}
