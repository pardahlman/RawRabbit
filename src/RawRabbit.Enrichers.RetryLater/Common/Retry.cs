using System;

namespace RawRabbit.Common
{
	public class Retry : Acknowledgement
	{
		public TimeSpan Span { get; set; }

		public Retry(TimeSpan span)
		{
			Span = span;
		}

		public static Retry In(TimeSpan span)
		{
			return new Retry(span);
		}
	}
}