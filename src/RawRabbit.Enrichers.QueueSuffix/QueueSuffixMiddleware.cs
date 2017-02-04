using System;
using System.Threading;
using System.Threading.Tasks;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;
using RawRabbit.Pipe.Middleware;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public class QueueSuffixMiddleware : StagedMiddleware
	{
		protected Func<IPipeContext, bool> ActivatedFlagFunc;
		protected Func<IPipeContext, QueueDeclaration> QueueDeclareFunc;
		protected Func<IPipeContext, string> CustomSuffixFunc;
		protected Func<IPipeContext, ConsumeConfiguration> ConsumeCfgFunc;
		protected Action<QueueDeclaration, string> AppendSuffixAction;
		public override string StageMarker => Pipe.StageMarker.ConsumeConfigured;

		public QueueSuffixMiddleware(QueueSuffixOptions options = null)
		{
			ActivatedFlagFunc = options?.ActiveFunc ?? (context => context.GetCustomQueueSuffixActivated());
			CustomSuffixFunc = options?.CustomSuffixFunc ?? (context => context.GetCustomQueueSuffix());
			QueueDeclareFunc = options?.QueueDeclareFunc ?? (context => context.GetQueueDeclaration());
			AppendSuffixAction = options?.AppendSuffixAction ?? ((queue, suffix) => queue.Name = $"{queue.Name}_{suffix}");
			ConsumeCfgFunc = options?.ConsumeConfigFunc ?? (context => context.GetConsumeConfiguration());
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var activated = GetActivatedFlag(context);
			if (!activated)
			{
				return Next.InvokeAsync(context, token);
			}
			var declaration = GetQueueDeclaration(context);
			var suffix = GetCustomQueueSuffix(context);
			AppendSuffix(declaration, suffix);
			var consumeConfig = GetConsumeConfig(context);
			AlignConsumerConfig(consumeConfig, declaration);
			return Next.InvokeAsync(context, token);
		}

		protected virtual void AlignConsumerConfig(ConsumeConfiguration consumeConfig, QueueDeclaration declaration)
		{
			if (consumeConfig == null)
			{
				return;
			}
			consumeConfig.QueueName = declaration.Name;
		}

		protected virtual bool GetActivatedFlag(IPipeContext context)
		{
			return ActivatedFlagFunc.Invoke(context);
		}

		protected virtual QueueDeclaration GetQueueDeclaration(IPipeContext context)
		{
			return QueueDeclareFunc?.Invoke(context);
		}

		protected virtual string GetCustomQueueSuffix(IPipeContext context)
		{
			return CustomSuffixFunc?.Invoke(context);
		}

		protected virtual void AppendSuffix(QueueDeclaration queue, string suffix)
		{
			AppendSuffixAction?.Invoke(queue, suffix);
		}

		protected virtual ConsumeConfiguration GetConsumeConfig(IPipeContext context)
		{
			return ConsumeCfgFunc(context);
		}
	}
}
