using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RawRabbit.Logging;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Publish.Middleware
{
	public class ReturnCallbackOptions
	{
		public Func<IPipeContext, EventHandler<BasicReturnEventArgs>> CallbackFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Action<IPipeContext, EventHandler<BasicReturnEventArgs>> PostInvokeAction { get; set; }
	}

	public class ReturnCallbackMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, EventHandler<BasicReturnEventArgs>> CallbackFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Action<IPipeContext, EventHandler<BasicReturnEventArgs>> PostInvoke;
		private readonly ILog _logger = LogProvider.For<ReturnCallbackMiddleware>();

		public ReturnCallbackMiddleware(ReturnCallbackOptions options = null)
		{
			CallbackFunc = options?.CallbackFunc ?? (context => context.GetReturnCallback());
			ChannelFunc = options?.ChannelFunc?? (context => context.GetTransientChannel());
			PostInvoke = options?.PostInvokeAction;
		}

		public override async Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var callback = GetCallback(context);
			if (callback == null)
			{
				_logger.Debug("No Mandatory Callback registered.");
				await Next.InvokeAsync(context, token);
				return;
			}

			var channel = GetChannel(context);
			if (channel == null)
			{
				_logger.Warn("Channel not found in Pipe Context. Mandatory Callback not registered.");
				await Next.InvokeAsync(context, token);
				return;
			}

			_logger.Debug("Register Mandatory Callback on channel {channelNumber}", channel.ChannelNumber);
			channel.BasicReturn += callback;
			PostInvoke?.Invoke(context, callback);

			await Next.InvokeAsync(context, token);
			_logger.Debug("Removing Mandatory Callback on channel {channelNumber}", channel.ChannelNumber);
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
