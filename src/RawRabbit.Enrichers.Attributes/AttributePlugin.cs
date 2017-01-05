using System;
using RawRabbit.Enrichers.Attributes.Middleware;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public static class AttributePlugin
	{
		private const string RequestMsgType = "RequestMessageType";
		private const string ResponseMsgType = "ResponseMessageType";

		public static IClientBuilder UseAttributeRouting(this IClientBuilder builder, PublishAttributeOptions publish = null, ConsumeAttributeOptions consume = null)
		{
			if (publish == null)
			{
				publish = new PublishAttributeOptions
				{
					MessageTypeFunc = context => context.Get<Type>(PipeKey.MessageType) ?? context.Get<Type>(RequestMsgType)
				};
			}
			if (consume == null)
			{
				consume = new ConsumeAttributeOptions
				{
					MessageTypeFunc = context => context.Get<Type>(PipeKey.MessageType) ?? context.Get<Type>(RequestMsgType)
				};
			}
			builder.Register(pipe => pipe
				.Use<PublishAttributeMiddleware>(publish)
				.Use<ConsumeAttributeMiddleware>(consume));
			return builder;
		}
	}
}
