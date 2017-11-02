using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Enrichers.MessageContext.Respond;
using RawRabbit.Operations.Respond.Acknowledgement;
using RawRabbit.Operations.Respond.Context;
using RawRabbit.Operations.Respond.Core;
using RawRabbit.Operations.Respond.Middleware;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit
{
	public static class RespondMessageContextExtension
	{
		public static Action<IPipeBuilder> RespondPipe = RespondExtension.RespondPipe + (pipe =>
		{
			pipe.Replace<ConsumerMessageHandlerMiddleware, ConsumerMessageHandlerMiddleware>(args: new ConsumeOptions
			{
				Pipe = RespondExtension.ConsumePipe + (consume => consume
					.Replace<RespondExceptionMiddleware, RespondExceptionMiddleware>(args: new RespondExceptionOptions
					{
						InnerPipe = p => p
							.Use<BodyDeserializationMiddleware>(new MessageDeserializationOptions
							{
								BodyTypeFunc = context => context.GetRequestMessageType()
							})
							.Use<StageMarkerMiddleware>(StageMarkerOptions.For(RespondStage.MessageDeserialized))
							.Use<HandlerInvocationMiddleware >(ResponseHandlerOptionFactory.Create(new HandlerInvocationOptions
							{
								HandlerArgsFunc = context => new[]
								{
									context.GetMessage(),
									context.GetMessageContextResolver()?.Invoke(context) ?? context.GetMessageContext()
								}
							}))
					})
					.Use<HeaderDeserializationMiddleware>(new HeaderDeserializationOptions
					{
						HeaderKeyFunc = c => PropertyHeaders.Context,
						HeaderTypeFunc = c => c.GetMessageContextType(),
						ContextSaveAction = (pipeCtx, msgCtx) => pipeCtx.Properties.TryAdd(PipeKey.MessageContext, msgCtx)
					}))
			});
		});

		public static Task<IPipeContext> RespondAsync<TRequest, TResponse, TMessageContext>(
			this IBusClient client,
			Func<TRequest, TMessageContext, Task<TResponse>> handler,
			Action<IRespondContext> context = null,
			CancellationToken ct = default(CancellationToken))
		{
			return client
				.RespondAsync<TRequest, TResponse, TMessageContext>((request, messageContext) => handler
					.Invoke(request, messageContext)
					.ContinueWith<TypedAcknowlegement<TResponse>>(t =>
					{
						if (t.IsFaulted)
							throw t.Exception;
						return new Ack<TResponse>(t.Result);
					}, ct), context, ct);
		}

		public static Task<IPipeContext> RespondAsync<TRequest, TResponse, TMessageContext>(
			this IBusClient client,
			Func<TRequest, TMessageContext, Task<TypedAcknowlegement<TResponse>>> handler,
			Action<IRespondContext> context = null,
			CancellationToken ct = default(CancellationToken))
		{
			return client
				.InvokeAsync(RespondPipe, ctx =>
				{
					Func<object[], Task> genericHandler = args => (handler((TRequest) args[0], (TMessageContext) args[1])
						.ContinueWith(tResponse =>
						{
							if (tResponse.IsFaulted)
								throw tResponse.Exception;
							return tResponse.Result.AsUntyped();
						}, ct));
					ctx.Properties.Add(RespondKey.IncomingMessageType, typeof(TRequest));
					ctx.Properties.Add(RespondKey.OutgoingMessageType, typeof(TResponse));
					ctx.AddMessageContextType<TMessageContext>();
					ctx.Properties.Add(PipeKey.MessageHandler, genericHandler);
					context?.Invoke(new RespondContext(ctx));
				}, ct);
		}
	}
}
