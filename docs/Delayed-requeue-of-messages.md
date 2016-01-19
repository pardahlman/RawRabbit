# Requeue with delay
Sometimes it can be handy to stop processing a message, and put it back on the queue and tell RabbitMq to redeliver it in X seconds/minutes/hours. The scenario might be

* Some operation in the message handler has thrown an exception, and as part of the error strategy, the message is retried.
* It is not desired to query subsystems due to maintenance, like nightly restores, database copies..

`RawRabbit` achieves this by using a [`dead letter exchange`](https://www.rabbitmq.com/dlx.html) in combination with the [`time to live`](https://www.rabbitmq.com/ttl.html) extension. The idea comes from [yuserinterface's blog](http://yuserinterface.com/dev/2013/01/08/how-to-schedule-delay-messages-with-rabbitmq-using-a-dead-letter-exchange/), and it is quite clever; a message that should be retried later is published to a "retry" exchange on a queue that has the actual exchange as its dead letter exchange and a time to live that matches the desired timespan.

```csharp
subscriber.SubscribeAsync<BasicMessage>(async (message, context) =>
{
	if (CanNotBeProcessed())
	{
		context.RetryLater(TimeSpan.FromMinutes(5));
		return;
	}
	// five minutes later we're here.
});
```

The advanced context has information about
* Original sent date, that is the `DateTime` when the message was first published
* Number of retries, that is how many times it has been retried. This is useful for error strategies such as "retry three times, then `Nack` it all together).

In order to use `RetryLater`, make sure you use an [advanced message context](https://github.com/pardahlman/RawRabbit/wiki/Chaining-messages#advanced-message-context).