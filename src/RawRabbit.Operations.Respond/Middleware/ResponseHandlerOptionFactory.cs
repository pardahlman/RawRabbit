using System.Threading.Tasks;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ResponseHandlerOptionFactory
	{
		public static HandlerInvokationOptions Create(HandlerInvokationOptions options = null)
		{
			return new HandlerInvokationOptions
			{
				HandlerArgsFunc = options?.HandlerArgsFunc ?? (context => new[] {context.GetMessage()}),
				PostInvokeAction = options?.PostInvokeAction ?? ((context, task) =>
				{
					if (task.IsFaulted)
					{
						return;
					}
					if (task is Task<object> responseTask)
					{
						context.Properties.TryAdd(RespondKey.ResponseMessage, responseTask.Result);
						return;
					}

					var ackTask = task as Task<Common.Acknowledgement>;
					if (ackTask?.Result is Ack ack)
					{
						context.Properties.TryAdd(RespondKey.ResponseMessage, ack.Response);
					}
				})
			};
		}
	}
}
