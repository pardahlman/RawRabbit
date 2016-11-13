namespace RawRabbit.Operations.Respond.Acknowledgement
{
	public abstract class TypedAcknowlegement<TResponse>
	{
		public abstract Common.Acknowledgement AsUntyped();
	}
}
