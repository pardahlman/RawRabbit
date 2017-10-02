using RawRabbit.Common;

namespace RawRabbit.Operations.Respond.Acknowledgement
{
	public class Reject<TResponse> : TypedAcknowlegement<TResponse>
	{
		public bool Requeue { get; set; }

		public Reject(bool requeue = true)
		{
			Requeue = requeue;
		}

		public override Common.Acknowledgement AsUntyped()
		{
			return new Reject(Requeue);
		}
	}
}