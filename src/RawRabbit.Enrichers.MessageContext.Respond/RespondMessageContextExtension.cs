using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class RespondMessageContextExtension
	{
		public static Action<IPipeBuilder> AutoAckPipe = RespondExtension.AutoAckPipe + (pipe =>
		{
			pipe.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
			{
				Pipe = RespondExtension.ConsumePipe + (consume => consume
						.Replace<RespondExceptionMiddleware, RespondExceptionMiddleware>(args: new RespondExceptionOptions
						{
							InnerPipe = p => p.Use<HandlerInvokationMiddleware>(new HandlerInvokationOptions
							{
								HandlerArgsFunc = context => new[] { context.GetMessage(), context.GetMessageContext() }
							})
						})
						.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
						{
							Type = typeof(IMessageContext),
							HeaderKey = PropertyHeaders.Context,
							ContextSaveAction = (pipeCtx, msgCtx) => pipeCtx.Properties.TryAdd(PipeKey.MessageContext, msgCtx)
						}))
			});
		});

		public static Task RespondAsync<TRequest, TResponse, TMessageContext>(this IBusClient client, Func<TRequest, TMessageContext, Task<TResponse>> handler, Action<IRespondConfigurationBuilder> configuration = null) where TMessageContext : IMessageContext
		{
			return client
				.InvokeAsync(AutoAckPipe, ctx =>
				{
					Func<object[], Task<object>> genericHandler = args => (handler((TRequest)args[0], (TMessageContext)args[1])
						.ContinueWith(tResponse => tResponse.Result as object));

					ctx.Properties.Add(RespondKey.RequestMessageType, typeof(TRequest));
					ctx.Properties.Add(RespondKey.ResponseMessageType, typeof(TResponse));
					ctx.Properties.Add(PipeKey.ConfigurationAction, configuration);
					ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
				});
		}
	}
}
