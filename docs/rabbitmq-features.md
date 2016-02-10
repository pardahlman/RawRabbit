# RabbitMq features

## Lazy Queues
As of `3.6.0` RabbitMq supports [Lazy Queues](https://www.rabbitmq.com/lazy-queues.html). To configure a specific queue as Lazy, simply use the optional configuration argument and set `QueueMode` to `lazy`.

```csharp
subscriber.SubscribeAsync<BasicMessage>((message, context) => 
//do stuff...
, cfg => cfg
	.WithQueue(q => q
		.WithArgument(QueueArgument.QueueMode, "lazy"))
);
```