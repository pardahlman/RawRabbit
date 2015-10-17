namespace RawRabbit.Core.Configuration
{
	public interface ISubscriptionConfiguration
	{
		ISubscriptionConfiguration WithTopic(string topic);
		ISubscriptionConfiguration WithAutoDelete(bool autoDelete = true);
		ISubscriptionConfiguration WithPrefetchCount(ushort count);
	}
}
