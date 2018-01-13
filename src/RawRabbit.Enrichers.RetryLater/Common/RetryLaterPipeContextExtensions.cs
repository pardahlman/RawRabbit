using RawRabbit.Pipe;

namespace RawRabbit.Common
{
	public static class RetryLaterPipeContextExtensions
	{
		private const string RetryInformationKey = "RetryInformation";

		internal static IPipeContext AddRetryInformation(this IPipeContext context, RetryInformation retryInformation)
		{
			context.Properties.TryAdd(RetryInformationKey, retryInformation);
			return context;
		}

		public static RetryInformation GetRetryInformation(this IPipeContext context)
		{
			return context.Get<RetryInformation>(RetryInformationKey);
		}
	}
}
