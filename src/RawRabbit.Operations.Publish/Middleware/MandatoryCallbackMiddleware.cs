using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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

		public MandatoryCallbackMiddleware(MandatoryCallbackOptions options = null)
		{
			CallbackFunc = options?.CallbackFunc ?? (context => context.GetReturnedMessageCallback());
			ChannelFunc = options?.ChannelFunc?? (context => context.GetTransientChannel());
			PostInvoke = options?.PostInvokeAction;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var callback = GetCallback(context);
			if (callback == null)
			{
				return Next.InvokeAsync(context);
			}

			var channel = GetChannel(context);
			if (channel == null)
			{
				return Next.InvokeAsync(context);
			}

			channel.BasicReturn += callback;
			PostInvoke?.Invoke(context, callback);

			return Next
				.InvokeAsync(context)
				.ContinueWith(t => channel.BasicReturn -= callback);
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
