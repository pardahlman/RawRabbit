using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware
{
	public class AckableGetResultOptions
	{
		public Func<IPipeContext, BasicGetResult> BasicGetFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; internal set; }
		public Action<IPipeContext, AckableGetResult> PostExecutionAction { get; internal set; }
	}

	public class AckableGetResultMiddleware : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, BasicGetResult> BasicGetFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Action<IPipeContext, AckableGetResult> PostExecutionAction;

		public AckableGetResultMiddleware(AckableGetResultOptions options = null)
		{
			BasicGetFunc = options?.BasicGetFunc ?? (context => context.GetBasicGetResult());
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetChannel());
			PostExecutionAction = options?.PostExecutionAction;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var channel = GetChannel(context);
			var getResult = GetBasicGetResult(context);
			var ackableGet = CreateAckableGet(channel, getResult);
			context.Properties.TryAdd(GetKeys.AckableGetResult, ackableGet);
			PostExecutionAction?.Invoke(context, ackableGet);
			return Next.InvokeAsync(context);
		}

		protected virtual AckableGetResult CreateAckableGet(IModel channel, BasicGetResult getResult)
		{
			return new AckableGetResult(channel, getResult);
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}

		protected virtual BasicGetResult GetBasicGetResult(IPipeContext context)
		{
			return BasicGetFunc(context);
		}
	}
}
