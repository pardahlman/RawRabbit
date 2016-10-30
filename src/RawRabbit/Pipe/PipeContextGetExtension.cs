namespace RawRabbit.Pipe
{
	public static class PipeContextGetExtension
	{
		public static TType Get<TType>(this IPipeContext context, string key, TType fallback = default(TType))
		{
			if (context?.Properties == null)
			{
				return fallback;
			}
			object result;
			if (context.Properties.TryGetValue(key, out result))
			{
				return result is TType ? (TType)result : fallback;
			}
			return fallback;
		}
	}
}
