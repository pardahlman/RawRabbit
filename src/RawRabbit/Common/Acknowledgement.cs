using System;

namespace RawRabbit.Common
{
	public abstract class Acknowledgement { }

	public class Ack : Acknowledgement
	{ }

	public class Nack : Acknowledgement
	{
		public bool Requeue { get; set; }

		public Nack(bool requeue = true)
		{
			Requeue = requeue;
		}
	}

	public class Reject : Acknowledgement
	{
		public bool Requeue { get; set; }

		public Reject(bool requeue = true)
		{
			Requeue = requeue;
		}
	}

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
