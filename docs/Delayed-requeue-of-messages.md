# Requeue with delay
`RawRabbit` supports requeing of messages with a predefined retry time interval. The feature uses the [`dead letter exchange`](https://www.rabbitmq.com/dlx.html) in combination with the [`time to live`](https://www.rabbitmq.com/ttl.html) extension. The idea comes from [yuserinterface's blog](http://yuserinterface.com/dev/2013/01/08/how-to-schedule-delay-messages-with-rabbitmq-using-a-dead-letter-exchange/); a message that should be retried later is published to a "retry" exchange on a queue that has the actual exchange as its dead letter exchange and a time to live that matches the desired timespan. In order to use `RetryLater`, make sure you use an [advanced message context](understanding-message-context.html#advanced-context).

## Later execution
```csharp
client.SubscribeAsync<BasicMessage>(async (message, context) =>
{
	if (CanNotBeProcessed())
	{
		context.RetryLater(TimeSpan.FromMinutes(5));
		return;
	}
	// five minutes later we're here.
});
```
## Error strategy
The advanced context has information about
* Original sent date, that is the `DateTime` when the message was first published
* Number of retries, that is how many times it has been retried. This is useful for error strategies such as "retry three times, then `Nack` it all together).

The requeue can also be used as an error strategy.

```csharp
client.SubscribeAsync<BasicMessage>(async (message, context) =>
{
	if (context.RetryInfo.NumberOfRetries > 10)
    {
	    throw new Exception($"Unable to handle message '{context.GlobalRequestId}'.");
    }
    // more code here...
});
```



