using System;
using System.Collections.Generic;
using RawRabbit.Enrichers.Attributes.Middleware;
using RawRabbit.Instantiation;
using RawRabbit.Pipe;

namespace RawRabbit
{
	public static class AttributePlugin
	{
		private const string RequestMsgType = "RequestMessageType";
		private const string ResponseMsgType = "ResponseMessageType";

		public static IClientBuilder UseAttributeRouting(this IClientBuilder builder, AttributeOptions consume = null)
		{
			if (consume == null)
			{
				consume = new AttributeOptions
				{
					MessageTypeFunc = context => new List<Type>
					{
						context.Get<Type>(PipeKey.MessageType),
						context.Get<Type>(RequestMsgType),
						context.Get<Type>(ResponseMsgType),
					}
				};
			}
			builder.Register(pipe => pipe
				.Use<AttributeMiddleware>(consume));
			return builder;
		}
	}
}
