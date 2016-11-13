using System;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Enrichers.MessageContext.Respond.Middleware;
using RawRabbit.Operations.Respond.Configuration;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class RespondMessageContextExtension
	{
		public static Action<IPipeBuilder> RespondPipe = RespondExtension.RespondPipe += pipe =>
		{
			pipe.Replace<MessageConsumeMiddleware, MessageConsumeMiddleware>(args: new ConsumeOptions
			{
				Pipe = RespondExtension.ConsumePipe += consume => consume
					.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
					{
						Type = typeof(IMessageContext),
						HeaderKey = PropertyHeaders.Context,
						ContextSaveAction = (pipeCtx, msgCtx) => pipeCtx.Properties.TryAdd(PipeKey.MessageContext, msgCtx)
					})
					.Replace<Operations.Respond.Middleware.AutoAckMessageHandlerMiddleware, AutoAckMessageHandlerMiddleware>()
			});
		};

		public static Task RespondAsync<TRequest, TResponse, TMessageContext>(this IBusClient client, Func<TRequest, TMessageContext, Task<TResponse>> handler, Action<IRespondConfigurationBuilder> configuration = null) where TMessageContext : IMessageContext
		{
			return client
				.InvokeAsync(RespondPipe, ctx =>
				{
					Func<object, IMessageContext, Task<object>> genericHandler = (req,c) => (handler((TRequest)req, (TMessageContext)c)
						.ContinueWith(tResponse => tResponse.Result as object));

					ctx.Properties.Add(RespondKey.RequestMessageType, typeof(TRequest));
					ctx.Properties.Add(RespondKey.ResponseMessageType, typeof(TResponse));
					ctx.Properties.Add(PipeKey.ConfigurationAction, configuration);
					ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
				});
		}
	}
}
