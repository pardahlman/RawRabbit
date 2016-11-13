using System;
using RawRabbit.Common;

namespace RawRabbit.Operations.Respond.Acknowledgement
{
	public class Retry<TResponse> : TypedAcknowlegement<TResponse>
	{
		public TimeSpan Span { get; set; }

		public static Retry<TResponse> In(TimeSpan span)
		{
			return new Retry<TResponse> { Span = span };
		}

		public override Common.Acknowledgement AsUntyped()
		{
			return new Retry(Span);
		}
	}
}
