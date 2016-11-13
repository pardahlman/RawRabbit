using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Configuration.Consume;
using RawRabbit.Operations.Publish;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class MessageContextPublishExtension
	{
		public static Action<IPipeBuilder> PublishPipeAction = PublishMessageExtension.PublishPipeAction += pipe => pipe
			.Use<HeaderSerializationMiddleware>(new HeaderSerializationOptions
			{
				HeaderKey = PropertyHeaders.Context,
				RetrieveItemFunc = context => context.GetMessageContext(),
				CreateItemFunc = context => { throw new KeyNotFoundException(PipeKey.MessageContext);}
			});

		public static Task PublishAsync<TMessage, TMessageContext>(
			this IBusClient busClient,
			TMessage message = default(TMessage),
			TMessageContext context = default(TMessageContext),
			Action<IPublishConfigurationBuilder> config = null,
			CancellationToken token = default(CancellationToken))
		{
			return busClient.InvokeAsync(
				PublishPipeAction,
				ctx =>
				{
					ctx.Properties.Add(PipeKey.MessageType, typeof(TMessage));
					ctx.Properties.Add(PipeKey.Message, message);
					ctx.Properties.Add(PipeKey.MessageContext, context);
					ctx.Properties.Add(PipeKey.Operation, PublishKey.Publish);
					ctx.Properties.Add(PipeKey.ConfigurationAction, config);
				}, token);
		}
	}
}
