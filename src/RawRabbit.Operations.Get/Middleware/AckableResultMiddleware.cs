using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Operations.Get.Model;
using RawRabbit.Pipe;

namespace RawRabbit.Operations.Get.Middleware
{
	public class AckableResultOptions<TResult>
	{
		public Func<IPipeContext, TResult> ContentFunc { get; set; }
		public Func<IPipeContext, IModel> ChannelFunc { get; internal set; }
		public Func<IPipeContext, ulong> DeliveryTagFunc { get; internal set; }
		public Action<IPipeContext, Ackable<TResult>> PostExecutionAction { get; internal set; }
	}

	public class AckableResultOptions : AckableResultOptions<object> { }

	public class AckableResultMiddleware : AckableResultMiddleware<object>
	{
		public AckableResultMiddleware(AckableResultOptions options) : base(options)
		{ }
	}

	public class AckableResultMiddleware<TResult> : Pipe.Middleware.Middleware
	{
		protected Func<IPipeContext, TResult> GetResultFunc;
		protected Func<IPipeContext, IModel> ChannelFunc;
		protected Action<IPipeContext, Ackable<TResult>> PostExecutionAction;
		protected Func<IPipeContext, ulong> DeliveryTagFunc;

		public AckableResultMiddleware(AckableResultOptions<TResult> options = null)
		{
			GetResultFunc = options?.ContentFunc;
			ChannelFunc = options?.ChannelFunc ?? (context => context.GetChannel());
			PostExecutionAction = options?.PostExecutionAction;
			DeliveryTagFunc = options?.DeliveryTagFunc;
		}

		public override Task InvokeAsync(IPipeContext context)
		{
			var channel = GetChannel(context);
			var getResult = GetResult(context);
			var deliveryTag = GetDeliveryTag(context);
			var ackableResult = CreateAckableResult(channel, getResult, deliveryTag);
			context.Properties.TryAdd(GetKey.AckableResult, ackableResult);
			PostExecutionAction?.Invoke(context, ackableResult);
			return Next.InvokeAsync(context);
		}

		protected virtual ulong GetDeliveryTag(IPipeContext context)
		{
			return DeliveryTagFunc(context);
		}

		protected virtual Ackable<TResult> CreateAckableResult(IModel channel, TResult result, ulong deliveryTag)
		{
			return new Ackable<TResult>(result, channel, deliveryTag);
		}

		protected virtual IModel GetChannel(IPipeContext context)
		{
			return ChannelFunc(context);
		}

		protected virtual TResult GetResult(IPipeContext context)
		{
			return GetResultFunc(context);
		}
	}
}
