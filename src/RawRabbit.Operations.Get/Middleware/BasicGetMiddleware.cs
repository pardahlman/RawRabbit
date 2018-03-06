using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware
{
	public class BasicGetOptions
	{
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> AutoAckFunc { get; internal set; }
		public Action<IPipeContext, BasicGetResult> PostExecutionAction { get; set; }
		public Func<IPipeContext, string> QueueNameFunc { get; internal set; }
	}

	public class BasicGetMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected  Func<IPipeContext, string> QueueNameFunc;
		protected Func<IPipeContext, bool> AutoAckFunc;
		protected Action<IPipeContext, BasicGetResult> PostExecutionAction;

		public BasicGetMiddleware(BasicGetOptions options = null)
		{
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetTransientChannel());
			QueueNameFunc = options?.QueueNameFunc ?? (context => context.GetGetConfiguration()?.QueueName);
			AutoAckFunc = options?.AutoAckFunc ?? (context => context.GetGetConfiguration()?.AutoAck ?? false);
			PostExecutionAction = options?.PostExecutionAction;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var channel = GetChannel(context);
			var queueNamme = GetQueueName(context);
			var autoAck = GetAutoAck(context);
			var getResult = PerformBasicGet(channel, queueNamme, autoAck);
			context.Properties.TryAdd(GetPipeExtensions.BasicGetResult, getResult);
			PostExecutionAction?.Invoke(context, getResult);
			return Next.InvokeAsync(context, token);
		}

		protected virtual BasicGetResult PerformBasicGet(IModel channel, string queueName, bool autoAck)
		{
			return channel.BasicGet(queueName, autoAck);
		}

		protected virtual bool GetAutoAck(IPipeContext context)
		{
			return AutoAckFunc(context);
		}

		protected virtual string GetQueueName(IPipeContext context)
		{
			return QueueNameFunc(context);
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}
	}
}
