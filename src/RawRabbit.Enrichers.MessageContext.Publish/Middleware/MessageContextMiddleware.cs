using System;
using System.IO;
using System.Threading.Tasks;
using RawRabbit.Common;
using RawRabbit.Context;
using RawRabbit.Operations.Publish;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;
using RawRabbit.Serialization;

namespace RawRabbit.Enrichers.MessageContext.Publish.Middleware
{
	public class MessageContextMiddleware<TMessageContext> : StagedMiddleware where TMessageContext : IMessageContext, new()
	{
		private readonly ISerializer _serializer;

		public MessageContextMiddleware(ISerializer serializer)
		{
			_serializer = serializer;
		}
		public override Task InvokeAsync(IPipeContext context)
		{
			var messageContext = context.GetMessageContext();
			if (messageContext == null)
			{
				messageContext = new TMessageContext
				{
					GlobalRequestId = Guid.NewGuid()
				};
				context.Properties.Add(PipeKey.MessageContext, messageContext);
			}

			var properties = context.GetBasicProperties();
			var serializedProps = _serializer.Serialize(messageContext);
			properties.Headers.Add(PropertyHeaders.Context, serializedProps);
			return Next.InvokeAsync(context);
		}

		public override string StageMarker => PublishStage.BasicPropertiesCreated.ToString();
	}
}
