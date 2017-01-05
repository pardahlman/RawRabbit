using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class RespondInvokationMiddleware : HandlerInvokationMiddleware
	{
		public RespondInvokationMiddleware(HandlerInvokationOptions options = null) : base(new HandlerInvokationOptions
		{
			HandlerArgsFunc = options?.HandlerArgsFunc ?? (context => new []{ context.GetMessage()}),
			PostInvokeAction = options?.PostInvokeAction ?? ((context, task) =>
			{
				if (task.IsFaulted)
				{
					return;
				}
				var responseTask = task as Task<object>;
				if (responseTask != null)
				{
					context.Properties.TryAdd(RespondKey.ResponseMessage, responseTask.Result);
					return;
				}

				var ackTask = task as Task<Common.Acknowledgement>;
				var ack = ackTask?.Result as Ack;
				if (ack != null)
				{
					context.Properties.TryAdd(RespondKey.ResponseMessage, ack.Response);
				}
			})
		})
		{ }
	}
}
