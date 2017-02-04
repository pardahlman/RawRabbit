using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RawRabbit.Common;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.GlobalExecutionId.Middleware
{
	public class PublishHeaderAppenderOptions
	{
		public Func<IPipeContext, IBasicProperties> BasicPropsFunc { get; set; }
		public Func<IPipeContext, string> GlobalExecutionIdFunc { get; set; }
		public Action<IBasicProperties, string> AppendHeaderAction { get; set; }
	}

	public class PublishHeaderAppenderMiddleware : StagedMiddleware
	{
		protected Func<IPipeContext, IBasicProperties> BasicPropsFunc;
		protected Func<IPipeContext, string> GlobalExecutionIdFunc;
		protected Action<IBasicProperties, string> AppendAction;
		public override string StageMarker => Pipe.StageMarker.BasicPropertiesCreated;

		public PublishHeaderAppenderMiddleware(PublishHeaderAppenderOptions options = null)
		{
			BasicPropsFunc = options?.BasicPropsFunc ?? (context => context.GetBasicProperties());
			GlobalExecutionIdFunc = options?.GlobalExecutionIdFunc ?? (context => context.GetGlobalExecutionId());
			AppendAction = options?.AppendHeaderAction ?? ((props, id) => props.Headers.TryAdd(PropertyHeaders.GlobalExecutionId, id));
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token = new CancellationToken())
		{
			var props = GetBasicProps(context);
			var id = GetGlobalExecutionId(context);
			AddIdToHeader(props, id);
			return Next.InvokeAsync(context, token);
		}

		protected virtual IBasicProperties GetBasicProps(IPipeContext context)
		{
			return BasicPropsFunc?.Invoke(context);
		}

		protected virtual string GetGlobalExecutionId(IPipeContext context)
		{
			return GlobalExecutionIdFunc?.Invoke(context);
		}

		protected virtual void AddIdToHeader(IBasicProperties props, string globalExecutionId)
		{
			AppendAction?.Invoke(props, globalExecutionId);
		}
	}
}
