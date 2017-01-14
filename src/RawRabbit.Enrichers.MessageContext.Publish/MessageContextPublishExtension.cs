using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Operations.Publish;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public static class MessageContextPublishExtension
	{
		public static Task PublishAsync<TMessage, TMessageContext>(
			this IBusClient busClient,
			TMessage message = default(TMessage),
			TMessageContext msgContext = default(TMessageContext),
			Action<IPipeContext> context = null,
			CancellationToken token = default(CancellationToken))
		{
			return busClient.InvokeAsync(
				PublishMessageExtension.PublishPipeAction,
				ctx =>
				{
					ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.Add(PipeKey.Message, message);
					ctx.Properties.Add(PipeKey.Operation, PublishKey.Publish);
					if(msgContext != null)
						ctx.Properties.Add(PipeKey.MessageContext, msgContext);
					context?.Invoke(ctx);
				}, token);
		}
	}
}
