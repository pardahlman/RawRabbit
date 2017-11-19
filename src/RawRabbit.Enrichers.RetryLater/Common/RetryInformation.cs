using System;

namespace RawRabbit.Common
{
	public class RetryInformation
	{
		public int NumberOfRetries { get; set; }
		public DateTime OriginalDelivered { get; set; }
	}
}
