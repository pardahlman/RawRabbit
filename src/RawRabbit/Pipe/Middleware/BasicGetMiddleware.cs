using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RawRabbit.Pipe.Middleware
{
	public class BasicGetOptions
	{
		public Func<IPipeContext, IModel> ChannelFunc { get; set; }
		public Func<IPipeContext, bool> NoAckFunc { get; internal set; }
		public Action<IPipeContext, BasicGetResult> PostExecutionAction { get; set; }
		public Func<IPipeContext, string> QueueNameFunc { get; internal set; }
	}

	public class BasicGetMiddleware : Middleware
	{
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected  Func<IPipeContext, string> QueueNameFunc;
		protected Func<IPipeContext, bool> NoAckFunc;
		protected Action<IPipeContext, BasicGetResult> PostExecutionAction;

		public BasicGetMiddleware(BasicGetOptions options = null)
		{
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetChannel());
			QueueNameFunc = options?.QueueNameFunc ?? (context => context.GetGetConfiguration()?.QueueName);
			NoAckFunc = options?.NoAckFunc ?? (context => context.GetGetConfiguration()?.NoAck ?? false);
			PostExecutionAction = options?.PostExecutionAction;
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var channel = GetChannel(context);
			var queueNamme = GetQueueName(context);
			var noAck = GetNoAck(context);
			var getResult = PerformBasicGet(channel, queueNamme, noAck);
			context.Properties.TryAdd(PipeKey.BasicGetResult, getResult);
			PostExecutionAction?.Invoke(context, getResult);
			return Next.InvokeAsync(context, token);
		}

		protected virtual BasicGetResult PerformBasicGet(IModel channel, string queueName, bool noAck)
		{
			return channel.BasicGet(queueName, noAck);
		}

		protected virtual bool GetNoAck(IPipeContext context)
		{
			return NoAckFunc(context);
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
