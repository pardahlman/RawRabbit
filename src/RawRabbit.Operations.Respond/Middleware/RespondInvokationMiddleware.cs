using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class RespondInvokationMiddleware : MessageHandlerInvokationMiddleware
	{
		public RespondInvokationMiddleware() : base(new MessageHandlerInvokationOptions
		{
			HandlerArgsFunc = context => new []{ context.GetMessage()},
			PostInvokeAction = (context, task) =>
			{
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
			}
		})
		{ }
	}
}
