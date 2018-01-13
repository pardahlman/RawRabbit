using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Operations.Respond.Middleware
{
	public class ResponseHandlerOptionFactory
	{
		public static HandlerInvocationOptions Create(HandlerInvocationOptions options = null)
		{
			return new HandlerInvocationOptions
			{
				HandlerArgsFunc = options?.HandlerArgsFunc ?? (context => new[] {context.GetMessage()}),
				PostInvokeAction = options?.PostInvokeAction ?? ((context, task) =>
				{
					if (task is Ack ack)
					{
						context.Properties.TryAdd(RespondKey.ResponseMessage, ack.Response);
					}
				})
			};
		}
	}
}
