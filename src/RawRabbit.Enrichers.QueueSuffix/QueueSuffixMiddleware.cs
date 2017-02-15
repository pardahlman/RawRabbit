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
		protected Func<string, bool> SkipSuffixFunc;
		protected Func<IPipeContext, string> ContextSuffixOverride;

		public override string StageMarker => Pipe.StageMarker.ConsumeConfigured;

		public QueueSuffixMiddleware(QueueSuffixOptions options = null)
		{
			ActivatedFlagFunc = options?.ActiveFunc ?? (context => context.GetCustomQueueSuffixActivated());
			CustomSuffixFunc = options?.CustomSuffixFunc ?? (context => context.GetCustomQueueSuffix());
			QueueDeclareFunc = options?.QueueDeclareFunc ?? (context => context.GetQueueDeclaration());
			AppendSuffixAction = options?.AppendSuffixAction ?? ((queue, suffix) => queue.Name = $"{queue.Name}_{suffix}");
			ConsumeCfgFunc = options?.ConsumeConfigFunc ?? (context => context.GetConsumeConfiguration());
			SkipSuffixFunc = options?.SkipSuffixFunc ?? (string.IsNullOrWhiteSpace);
			ContextSuffixOverride = options?.ContextSuffixOverrideFunc ?? (context => null);
		}

		public override Task InvokeAsync(IPipeContext context, CancellationToken token)
		{
			var activated = GetActivatedFlag(context);
			if (!activated)
			{
				return Next.InvokeAsync(context, token);
			}
			var declaration = GetQueueDeclaration(context);
			if (declaration == null)
			{
				return Next.InvokeAsync(context, token);
			}
			var suffix = GetCustomQueueSuffix(context);
			if (SkipSuffix(suffix))
			{
				return Next.InvokeAsync(context, token);
			}
			AppendSuffix(declaration, suffix);
			var consumeConfig = GetConsumeConfig(context);
			AlignConsumerConfig(consumeConfig, declaration);
			return Next.InvokeAsync(context, token);
		}

		protected virtual bool SkipSuffix(string suffix)
		{
			return SkipSuffixFunc.Invoke(suffix);
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
			var suffixOverride = GetContextSuffixOverride(context);
			if (!string.IsNullOrWhiteSpace(suffixOverride))
			{
				return suffixOverride;
			}
			return CustomSuffixFunc?.Invoke(context);
		}

		protected virtual string GetContextSuffixOverride(IPipeContext context)
		{
			return ContextSuffixOverride?.Invoke(context);
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
