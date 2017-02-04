using System;
using RawRabbit.Configuration.Consume;
using RawRabbit.Configuration.Queue;
using RawRabbit.Pipe;

namespace RawRabbit.Enrichers.QueueSuffix
{
	public class QueueSuffixOptions
	{
		public Func<IPipeContext, QueueDeclaration> QueueDeclareFunc;
		public Func<IPipeContext, string> CustomSuffixFunc;
		public Func<IPipeContext, bool> ActiveFunc;
		public Func<IPipeContext, ConsumeConfiguration> ConsumeConfigFunc;
		public Action<QueueDeclaration, string> AppendSuffixAction;
	}
}