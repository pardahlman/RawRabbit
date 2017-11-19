using System;

namespace RawRabbit.Operations.Respond.Acknowledgement
{
	public static class Respond
	{
		public static Ack<TResponse> Ack<TResponse>(TResponse response)
		{
			return new Ack<TResponse>(response);
		}

		public static Nack<TResponse> Nack<TResponse>(bool requeue = true)
		{
			return new Nack<TResponse>(requeue);
		}

		public static Reject<TResponse> Reject<TResponse>(bool requeue = true)
		{
			return new Reject<TResponse>(requeue);
		}
	}
}
